#!/usr/bin/env python3
"""Ensure useful browser stage diagnostics cannot publish arbitrary call logs."""
import importlib.util
from pathlib import Path
import tempfile
import sys
import unittest
import xml.etree.ElementTree as ET

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('target', Path(__file__).with_name('start-e2e-target.py'))
target = importlib.util.module_from_spec(spec)
spec.loader.exec_module(target)

class SanitizerTests(unittest.TestCase):
    def test_only_allowlisted_stages_survive(self):
        with tempfile.TemporaryDirectory() as directory:
            report = Path(directory) / 'junit.xml'
            report.write_text('<testsuites><testsuite><properties><property value="SECRET_CANARY"/></properties><testcase><failure message="SECRET_CANARY">SECRET_CANARY NEXORA_E2E_STEP[GOAL_PROGRESS]</failure><system-out>SECRET_CANARY</system-out></testcase><testcase><error>SECRET_CANARY NEXORA_E2E_STEP[UNTRUSTED_STAGE]</error></testcase></testsuite></testsuites>')
            target.sanitize_junit(report)
            content = report.read_text()
            self.assertNotIn('SECRET_CANARY', content)
            self.assertNotIn('UNTRUSTED_STAGE', content)
            self.assertNotIn('system-out', content)
            self.assertNotIn('properties', content)
            self.assertIn('Safe failing stage: GOAL_PROGRESS', content)
            self.assertFalse(ET.parse(report).find('.//failure').attrib)
            target.sanitize_junit(report)
            self.assertEqual(content, report.read_text())

if __name__ == '__main__':
    unittest.main()
