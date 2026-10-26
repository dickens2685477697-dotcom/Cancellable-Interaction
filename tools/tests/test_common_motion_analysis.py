import importlib.util
from pathlib import Path
import unittest

module_path = Path(__file__).resolve().parents[1] / "analyze_magic_tap_comparison.py"
spec = importlib.util.spec_from_file_location("legacy_magic_comparison", module_path)
comparison = importlib.util.module_from_spec(spec)
spec.loader.exec_module(comparison)


class LegacyComparisonVersionChecks(unittest.TestCase):
    def test_original_sessions_remain_eligible(self):
        comparison.ensure_legacy_session([{"mode": "magicTap2", "schemaVersion": "2"}])

    def test_common_motion_is_not_replayed_with_legacy_filter(self):
        for mode in ("magicTap2", "ixdlDynamic"):
            with self.subTest(mode=mode), self.assertRaisesRegex(ValueError, "common-motion"):
                comparison.ensure_legacy_session([{"mode": mode, "motionRulesVersion": "shared-provider-v2"}])

    def test_mixed_versions_are_rejected(self):
        with self.assertRaises(ValueError):
            comparison.ensure_legacy_session([{}, {"motionRulesVersion": "shared-provider-v2"}])


if __name__ == "__main__":
    unittest.main()
