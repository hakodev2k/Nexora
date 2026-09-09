#!/usr/bin/env python3
"""Validate vendored agent configuration without application dependencies."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import unittest

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]


def main():
    routes = json.loads((ROOT / '.ai/routing.json').read_text())
    required = {'baseline', 'architecture', 'backend', 'frontend', 'security',
                'database', 'cache', 'jobs', 'owner-isolation', 'verification'}
    if not required.issubset(routes):
        raise ValueError('missing required routing categories')
    covered = set()
    for route, paths in routes.items():
        if not isinstance(paths, list) or not paths:
            raise ValueError(f'empty or invalid route: {route}')
        for path in paths:
            target = (ROOT / path).resolve()
            if not target.is_relative_to(ROOT) or not target.is_file() or not target.read_text().strip():
                raise ValueError(f'missing, empty or escaping route: {path}')
            covered.add(path)
    for group in ('rules', 'skills'):
        for path in (ROOT / '.ai' / group).rglob('*.md'):
            if str(path.relative_to(ROOT)) not in covered:
                raise ValueError(f'unrouted specialist asset: {path}')
    skill = (ROOT / '.agents/skills/nexora-engineering/SKILL.md').read_text()
    if not skill.startswith('---\nname: nexora-engineering\ndescription: ') or '\n---\n' not in skill[4:]:
        raise ValueError('native skill metadata missing')
    if '.agents/skills/nexora-engineering/SKILL.md' not in (ROOT / 'AGENTS.md').read_text():
        raise ValueError('root entry point does not load the native skill')
    role = ROOT / '.ai/roles/technical-lead'
    boundary = ROOT / '.ai/controls/agent-multi-tenant-data-boundary-gate'
    completion = ROOT / '.ai/guards/agent-ground-truth-completion-gate'
    subprocess.run([sys.executable, str(role/'scripts/check-package.py'), str(role)], check=True)
    subprocess.run([sys.executable, str(boundary/'scripts/verify_package.py')], check=True)
    suite = unittest.TestSuite()
    for index, path in enumerate((boundary/'tests/test_tenant_boundary_gate.py', completion/'tests/test_completion_gate.py')):
        spec = importlib.util.spec_from_file_location(f'baseline_tests_{index}', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        tests = [value for name, value in vars(module).items() if name.startswith('test_') and callable(value)]
        if not tests:
            raise ValueError(f'no regression tests discovered: {path}')
        suite.addTests(unittest.FunctionTestCase(test) for test in tests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    if not result.wasSuccessful():
        return 1
    print(f'Baseline verified: {len(routes)} routes; {result.testsRun} gate tests. Application tests not run.')
    return 0


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except (ValueError, OSError, TypeError, subprocess.CalledProcessError) as exc:
        print(f'Baseline failed: {exc}', file=sys.stderr)
        raise SystemExit(1)
