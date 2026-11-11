import csv
from pathlib import Path
import tempfile
import unittest
from analyze_sessions import normal_standard_time, summarize


class AnalysisTests(unittest.TestCase):
    def test_rating_only_manual_work_and_declared_allowance(self):
        normal, standard = normal_standard_time(40, 20, 100, 0.15)
        self.assertEqual(normal, 60)
        self.assertAlmostEqual(standard, 70.58823529411765)
        self.assertEqual(normal_standard_time(40, 20, 120, 0)[0], 68)
        with self.assertRaises(ValueError):
            normal_standard_time(40, 20, 100, 1)

    def test_practice_aborts_and_input_modes_not_pooled(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for i, (layout, mode, practice, status) in enumerate([
                ('A', 'XR', False, 'Completed'), ('B', 'XR', False, 'Completed'),
                ('A', 'Desktop', False, 'Completed'), ('B', 'XR', True, 'Completed'),
                ('B', 'XR', False, 'Aborted'), ('A', 'XR', False, 'Completed'),
                ('C', 'XR', False, 'Completed')]):
                folder = root / str(i); folder.mkdir()
                run = dict(schema_version='1', run_id=str(i), participant_id='P01', layout_id=layout,
                           build_id='v2' if i == 5 else 'v1', input_mode=mode, practice=str(practice), status=status, active_seconds='10', condition_order='ABC' if layout == 'C' else 'AB')
                task = dict(run_id=str(i), pickup_s='2', transfer_s='3', placement_s='4', return_s='1', cycle_s='10',
                            outcome='completed' if status == 'Completed' else 'aborted', collisions='1', wrong_targets='0',
                            slew_degrees='20', trolley_m='4', hoist_m='5', load_path_m='8')
                for name, row in [('runs.csv', run), ('tasks.csv', task)]:
                    with (folder / name).open('w', newline='') as output:
                        writer = csv.DictWriter(output, fieldnames=row); writer.writeheader(); writer.writerow(row)
            inventory, groups, pairs, warnings = summarize(root)
            self.assertEqual(len(inventory), 7)
            self.assertEqual(sum(r['included'] for r in inventory), 5)
            self.assertEqual(len(groups), 5)
            self.assertEqual(next(g for g in groups if g['layout_id'] == 'C')['condition_orders'], 'ABC')
            self.assertEqual(len(pairs), 1)
            self.assertEqual(pairs[0]['input_mode'], 'XR')
            self.assertFalse(warnings)


if __name__ == '__main__':
    unittest.main()
