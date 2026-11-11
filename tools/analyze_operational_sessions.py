#!/usr/bin/env python3
"""Summarise op1/op2/op3 practice logs without pooling different tasks or test modes."""
import argparse
import csv
import math
import statistics
from collections import defaultdict
from pathlib import Path

KEYS = ('schema_version', 'build_id', 'layout_id', 'intake_mode', 'input_mode',
        'container_count', 'practice', 'automated', 'traffic_mode')


def read_rows(path):
    with path.open(newline='', encoding='utf-8-sig') as stream:
        return list(csv.DictReader(stream))


def number(value):
    result = float(value)
    if not math.isfinite(result) or result < 0:
        raise ValueError('duration must be finite and non-negative')
    return result


def analyze(root):
    groups, moves, warnings = defaultdict(list), defaultdict(list), []
    riskdata = defaultdict(list)
    for path in sorted(root.rglob('runs.csv')):
        try:
            rows = read_rows(path)
            if len(rows) != 1:
                raise ValueError('expected one run per folder')
            row = rows[0]
            if row.get('schema_version') not in ('op1', 'op2', 'op3'):
                continue
            row.setdefault('layout_id', 'SINGLE')
            row.setdefault('intake_mode', 'FullIntake')
            row.setdefault('traffic_mode', 'None')
            elapsed = number(row['active_seconds'])
            phases = read_rows(path.with_name('phase_times.csv'))
            if abs(sum(number(p['seconds']) for p in phases) - elapsed) > .02:
                warnings.append(f'{path.parent.name}: phase clock does not reconcile')
            key = tuple(row.get(k, 'unknown') for k in KEYS)
            # Aborted and in-progress work is counted separately from completed observations.
            groups[key].append((row['status'], elapsed))
            if row['schema_version'] in ('op2', 'op3'):
                records = read_rows(path.with_name('moves.csv'))
                ids = [m['move_id'] for m in records]
                if len(ids) != len(set(ids)):
                    warnings.append(f'{path.parent.name}: duplicate move IDs')
                for move in records:
                    total = number(move['move_s'])
                    if abs(sum(number(move[k]) for k in ('reposition_s', 'pickup_s', 'transfer_s', 'placement_s')) - total) > .02:
                        warnings.append(f'{path.parent.name}/{move["move_id"]}: move elements do not reconcile')
                    if row['schema_version'] == 'op3':
                        waits = number(move['crossing_wait_s']) + number(move.get('protected_stop_s', '0'))
                        if waits > total + .02:
                            warnings.append(f'{path.parent.name}/{move["move_id"]}: delay subsets exceed move time')
                    if row['status'] == 'Completed' and move['outcome'] == 'completed':
                        move_key = key + tuple(move[k] for k in ('move_id', 'cargo_id', 'operation', 'source_tier', 'target_tier'))
                        moves[move_key].append(total)
            if row['schema_version'] == 'op3':
                delays = {d['cause']: number(d['seconds']) for d in read_rows(path.with_name('delays.csv'))}
                if sum(delays.values()) > elapsed + .02:
                    warnings.append(f'{path.parent.name}: delay subsets exceed active time')
                incidents = read_rows(path.with_name('incidents.csv'))
                episode_ids = [i['episode_id'] for i in incidents]
                if len(episode_ids) != len(set(episode_ids)):
                    warnings.append(f'{path.parent.name}: duplicate incident episode IDs')
                for item in incidents:
                    if abs(number(item['end_s']) - number(item['start_s']) - number(item['duration_s'])) > .02:
                        warnings.append(f'{path.parent.name}/{item["episode_id"]}: incident duration does not reconcile')
                if row['status'] == 'Completed':
                    riskdata[key].append(dict(crossing=delays.get('CrossingWait', 0), stop=delays.get('ProtectedStop', 0),
                                             contacts=sum(i['type'] == 'Contact' for i in incidents),
                                             near=sum(i['type'] == 'NearMiss' for i in incidents),
                                             stops=sum(i['type'] in ('RestrictedZone', 'OperatorStop') or i['type'] == 'Contact' and i['severity'] in ('Strong', 'PersonContact') for i in incidents)))
        except (OSError, ValueError, KeyError, TypeError) as error:
            warnings.append(f'{path}: {error}')
    summary, move_summary = [], []
    for key, observations in sorted(groups.items()):
        completed = [t for state, t in observations if state == 'Completed']
        summary.append(dict(zip(KEYS, key), runs=len(observations), completed=len(completed),
                            aborted=sum(state == 'Aborted' for state, _ in observations),
                            unfinished=sum(state not in ('Completed', 'Aborted') for state, _ in observations),
                            mean_active_s=statistics.mean(completed) if completed else '',
                            sd_active_s=statistics.stdev(completed) if len(completed) > 1 else ''))
    for row in summary:
        key = tuple(row[k] for k in KEYS)
        observations = riskdata.get(key, [])
        for field, source in [('mean_crossing_wait_s', 'crossing'), ('mean_protected_stop_s', 'stop'),
                              ('completed_contact_episodes', 'contacts'), ('completed_near_misses', 'near'), ('completed_protected_stops', 'stops')]:
            values = [r[source] for r in observations]
            row[field] = (statistics.mean(values) if field.startswith('mean_') else sum(values)) if values else ''
    extra = ('move_id', 'cargo_id', 'operation', 'source_tier', 'target_tier')
    for key, times in sorted(moves.items()):
        move_summary.append(dict(zip(KEYS + extra, key), n=len(times), mean_observed_s=statistics.mean(times),
                                 sd_observed_s=statistics.stdev(times) if len(times) > 1 else ''))
    return summary, move_summary, warnings


def write_csv(path, rows):
    if not rows:
        return
    with path.open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('sessions', type=Path, help='OperationalSessions folder')
    parser.add_argument('--output', type=Path, default=Path('operational_analysis'))
    args = parser.parse_args()
    if not args.sessions.is_dir():
        parser.error('sessions folder does not exist')
    summary, moves, warnings = analyze(args.sessions)
    args.output.mkdir(parents=True, exist_ok=True)
    write_csv(args.output / 'shipment_summary.csv', summary)
    write_csv(args.output / 'move_summary.csv', moves)
    (args.output / 'checks.txt').write_text('\n'.join(warnings) + ('\n' if warnings else 'No reconciliation warnings.\n'))
    print(f'{len(summary)} separate task groups; {len(moves)} move groups; {len(warnings)} warnings.')
    print(f'Output: {args.output.resolve()}')
    print('Observed practice durations only. Automated tests are separated. No standard time or fatigue claim is inferred.')


if __name__ == '__main__':
    main()
