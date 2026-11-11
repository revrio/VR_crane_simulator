#!/usr/bin/env python3
"""Summarize PortLab exports without treating repeated cycles as independent people."""
import argparse
import csv
from collections import defaultdict
from pathlib import Path
from statistics import mean, median, stdev

PHASES = ('pickup_s', 'transfer_s', 'placement_s', 'return_s')


def normal_standard_time(manual_observed_s, fixed_process_s, rating_percent, allowance_fraction):
    """Sequential work only; allowance is a declared whole-cycle fraction of standard time."""
    if min(manual_observed_s, fixed_process_s) < 0 or rating_percent <= 0 or not 0 <= allowance_fraction < 1:
        raise ValueError('Times must be nonnegative, rating positive, and allowance between 0 and 1.')
    normal = manual_observed_s * rating_percent / 100 + fixed_process_s
    return normal, normal / (1 - allowance_fraction)


def read_rows(path):
    with path.open(newline='', encoding='utf-8-sig') as source:
        return list(csv.DictReader(source))


def summarize(root, include_practice=False):
    grouped = defaultdict(list)
    inventory, warnings = [], []
    for metadata in sorted(root.rglob('runs.csv')):
        try:
            rows = read_rows(metadata)
            if len(rows) != 1 or rows[0]['schema_version'] != '1':
                raise ValueError('expected one schema-1 run row')
            run = rows[0]
            tasks = read_rows(metadata.with_name('tasks.csv'))
            if any(t['run_id'] != run['run_id'] for t in tasks):
                raise ValueError('task/run ID mismatch')
            for task in tasks:
                if abs(sum(float(task[p]) for p in PHASES) - float(task['cycle_s'])) > 0.02:
                    raise ValueError('phase/cycle durations do not reconcile')
            practice = run['practice'].lower() == 'true'
            eligible = run['status'] == 'Completed' and all(t['outcome'] == 'completed' for t in tasks) and bool(tasks)
            inventory.append(dict(run, included=eligible and (include_practice or not practice), source=str(metadata.parent)))
            if not eligible or (practice and not include_practice):
                continue
            if abs(sum(float(t['cycle_s']) for t in tasks) - float(run['active_seconds'])) > 0.1:
                raise ValueError('run and task active times do not reconcile')
            key = run['participant_id'], run['layout_id'], run['input_mode'], run.get('build_id', 'legacy-unspecified')
            grouped[key].append((run, tasks))
        except (KeyError, ValueError, OSError) as error:
            if inventory and inventory[-1]['source'] == str(metadata.parent):
                inventory[-1]['included'] = False
            warnings.append(str(metadata) + ': ' + str(error))
    summaries = []
    for (participant, layout, mode, build), runs in sorted(grouped.items()):
        tasks = [task for _, cycle_tasks in runs for task in cycle_tasks]
        times = [float(t['cycle_s']) for t in tasks]
        row = dict(participant_id=participant, layout_id=layout, input_mode=mode, build_id=build,
                   runs=len(runs), cycles=len(tasks), mean_cycle_s=mean(times), median_cycle_s=median(times),
                   sd_cycle_s=stdev(times) if len(times) > 1 else '',
                   collisions=sum(int(t['collisions']) for t in tasks),
                   wrong_targets=sum(int(t['wrong_targets']) for t in tasks),
                   condition_orders=';'.join(sorted({r['condition_order'] for r, _ in runs})))
        for phase in PHASES:
            row['mean_' + phase] = mean(float(t[phase]) for t in tasks)
        for field in ('slew_degrees', 'trolley_m', 'hoist_m', 'load_path_m'):
            row['mean_' + field] = mean(float(t[field]) for t in tasks)
        summaries.append(row)
    by_person = defaultdict(dict)
    for row in summaries:
        by_person[row['participant_id'], row['input_mode'], row['build_id']][row['layout_id']] = row
    pairs = []
    for (participant, mode, build), conditions in sorted(by_person.items()):
        if 'A' not in conditions or 'B' not in conditions:
            continue
        a, b = conditions['A'], conditions['B']
        pairs.append(dict(participant_id=participant, input_mode=mode, build_id=build, A_mean_cycle_s=a['mean_cycle_s'],
                          B_mean_cycle_s=b['mean_cycle_s'], B_minus_A_s=b['mean_cycle_s'] - a['mean_cycle_s'],
                          time_reduction_percent=100 * (a['mean_cycle_s'] - b['mean_cycle_s']) / a['mean_cycle_s'] if a['mean_cycle_s'] else '',
                          A_collisions=a['collisions'], B_collisions=b['collisions'], A_wrong_targets=a['wrong_targets'], B_wrong_targets=b['wrong_targets']))
    return inventory, summaries, pairs, warnings


def write_table(path, rows):
    if not rows:
        path.write_text('No eligible records.\n')
        return
    with path.open('w', newline='', encoding='utf-8') as destination:
        writer = csv.DictWriter(destination, fieldnames=list(rows[0]))
        writer.writeheader()
        for row in rows:
            # IDs originate in user input; keep a spreadsheet from interpreting them as formulas.
            writer.writerow({k: ('\t' + v if isinstance(v, str) and v[:1] in ('=', '+', '-', '@') else v) for k, v in row.items()})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('sessions', type=Path, help='ResearchSessions folder or one run folder')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--include-practice', action='store_true', help='For technical validation only, not participant findings')
    args = parser.parse_args()
    if not args.sessions.is_dir():
        parser.error('sessions must be an existing directory')
    inventory, summaries, pairs, warnings = summarize(args.sessions, args.include_practice)
    args.output.mkdir(parents=True, exist_ok=True)
    write_table(args.output / 'run_inventory.csv', inventory)
    write_table(args.output / 'participant_layout_summary.csv', summaries)
    write_table(args.output / 'paired_layout_comparison.csv', pairs)
    report = [ '# PortLab time-study summary', '',
               'Practice data included: ' + str(args.include_practice) + '.',
               'Participant-layout groups: ' + str(len(summaries)) + '; paired participant/input-mode/build comparisons: ' + str(len(pairs)) + '.', '',
               'See run_inventory.csv for incomplete, aborted, practice, and included runs. Successful-cycle timing must be interpreted alongside these outcomes.', '',
               'Repeated cycles are summarized within participant, input mode and build version. Desktop, XR and different simulator versions are kept separate. A negative B-minus-A value means B was faster. These are descriptive pilot results, not an industrial production standard.', '',
               'Normal/standard time requires separately identified rateable manual work, fixed process time, a justified rating, and a stated allowance convention. The script does not infer these from crane transfer duration.' ]
    if warnings:
        report += ['', 'Data checks requiring attention:', ''] + ['- ' + w for w in warnings]
    (args.output / 'report.md').write_text('\n'.join(report) + '\n')
    print('Wrote', args.output.resolve(), '| groups:', len(summaries), '| paired participant/build groups:', len(pairs), '| warnings:', len(warnings))


if __name__ == '__main__':
    main()
