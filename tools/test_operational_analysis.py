import csv
import tempfile
import unittest
from pathlib import Path
from analyze_operational_sessions import analyze


class OperationalAnalysisTests(unittest.TestCase):
    def make_run(self, root, name, layout='C', intake='Staged', automated='False', status='Completed'):
        folder = root / name
        folder.mkdir()
        def write(name, fields, rows):
            with (folder / name).open('w', newline='') as stream:
                writer = csv.writer(stream)
                writer.writerow(fields.split(','))
                writer.writerows(rows)
        write('runs.csv', 'schema_version,build_id,run_id,practice,automated,input_mode,container_count,layout_id,intake_mode,active_seconds,status',
              [['op2', 'i2', name, 'True', automated, 'Desktop', 5, layout, intake, 12, status]])
        write('phase_times.csv', 'schema_version,run_id,phase,seconds', [['op2', name, 'Transfer', 12]])
        write('moves.csv', 'schema_version,run_id,move_id,cargo_id,source_slot,target_slot,operation,source_tier,target_tier,reposition_s,pickup_s,transfer_s,placement_s,move_s,outcome',
              [['op2', name, 'M001', 'C2', 'Y-C2', 'TEMP-01', 'Rehandle', 1, 0, 0, 1, 4, 1, 6, 'completed'],
               ['op2', name, 'M005', 'C2', 'TEMP-01', 'SHIP-01-T2', 'Ship', 0, 1, 1, 1, 3, 1, 6, 'completed']])
        return folder

    def test_groups_keep_task_and_test_modes_separate(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.make_run(root, 'staged')
            self.make_run(root, 'staged-repeat')
            self.make_run(root, 'full', intake='FullIntake')
            self.make_run(root, 'test', automated='True')
            self.make_run(root, 'layout', layout='D')
            summary, moves, warnings = analyze(root)
            self.assertEqual(len(summary), 4)
            self.assertEqual(len(moves), 8)
            self.assertFalse(warnings)
            self.assertEqual(max(r['completed'] for r in summary), 2)
            self.assertEqual(len({r['move_id'] for r in moves}), 2)

    def test_aborted_runs_are_not_complete_observations(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.make_run(root, 'partial', status='Aborted')
            summary, moves, warnings = analyze(root)
            self.assertEqual(summary[0]['aborted'], 1)
            self.assertEqual(summary[0]['mean_active_s'], '')
            self.assertFalse(moves)
            self.assertFalse(warnings)

    def test_mismatched_clock_reported(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            folder = self.make_run(root, 'bad')
            (folder / 'phase_times.csv').write_text('schema_version,run_id,phase,seconds\nop2,bad,Transfer,1\n')
            self.assertIn('phase clock does not reconcile', analyze(root)[2][0])

class OperationalRiskAnalysisTests(unittest.TestCase):
    make_run = OperationalAnalysisTests.make_run
    def make_op3(self, root, name, traffic='ControlledCrossing'):
        folder = self.make_run(root, name)
        from analyze_operational_sessions import read_rows
        def rewrite(path, rows):
            with path.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
                writer.writeheader()
                writer.writerows(rows)
        runs = read_rows(folder / 'runs.csv')
        runs[0].update(schema_version='op3', traffic_mode=traffic)
        rewrite(folder / 'runs.csv', runs)
        moves = read_rows(folder / 'moves.csv')
        for i, move in enumerate(moves):
            move.update(schema_version='op3', crossing_wait_s=str(i), protected_stop_s=str(1-i))
        rewrite(folder / 'moves.csv', moves)
        (folder / 'delays.csv').write_text('schema_version,run_id,cause,seconds\nop3,test,CrossingWait,1\nop3,test,ProtectedStop,1\n')
        (folder / 'incidents.csv').write_text('schema_version,episode_id,type,severity,start_s,end_s,duration_s\nop3,E001,Contact,Strong,1,2,1\nop3,E002,NearMiss,NearMiss,3,4,1\n')
        return folder

    def test_traffic_modes_and_incident_types_remain_separate(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.make_op3(root, 'normal')
            self.make_op3(root, 'drill', 'IntrusionDrill')
            rows, moves, warnings = analyze(root)
            self.assertEqual(len(rows), 2)
            self.assertEqual(len(moves), 4)
            self.assertFalse(warnings)
            for row in rows:
                self.assertEqual(row['completed_contact_episodes'], 1)
                self.assertEqual(row['completed_near_misses'], 1)
                self.assertEqual(row['completed_protected_stops'], 1)
                self.assertEqual(row['mean_active_s'], 12)
                self.assertEqual(row['mean_protected_stop_s'], 1)

    def test_delay_subsets_cannot_exceed_elapsed_time(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            folder = self.make_op3(root, 'bad')
            (folder / 'delays.csv').write_text('schema_version,run_id,cause,seconds\nop3,test,ProtectedStop,25\n')
            self.assertTrue(any('subsets exceed active time' in w for w in analyze(root)[2]))


if __name__ == '__main__':
    unittest.main()
