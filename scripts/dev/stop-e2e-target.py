#!/usr/bin/env python3
"""Stop only processes and private files proven to belong to this CI target."""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import sys
import time

def same_process(record):
    try:
        proc = Path(f'/proc/{record["pid"]}')
        stat = (proc / 'stat').read_text().rsplit(')', 1)[1].split()
        return stat[19] == record['start'] and hashlib.sha256((proc / 'cmdline').read_bytes()).hexdigest() == record['commandHash'] and os.getpgid(record['pid']) == record['pid']
    except (FileNotFoundError, ProcessLookupError): return False

def main():
    if not os.environ.get('NEXORA_E2E_TARGET_STATE'): return 0
    if os.environ.get('GITHUB_ACTIONS') != 'true' or os.environ.get('CI') != 'true': raise RuntimeError('CI identity required')
    run, attempt, job = (os.environ.get(k, '') for k in ('GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'GITHUB_JOB'))
    if not re.fullmatch('[0-9]+', run) or not re.fullmatch('[0-9]+', attempt) or not re.fullmatch('[A-Za-z0-9_-]+', job): raise RuntimeError('CI identity invalid')
    state_path = Path(os.environ['NEXORA_E2E_TARGET_STATE'])
    if not state_path.exists(): return 0
    root = state_path.parent
    if root.is_symlink() or state_path.is_symlink() or state_path.name != 'state.json' or root.parent.resolve() != Path(os.environ['RUNNER_TEMP']).resolve() or not root.name.startswith(f'nexora-ci-e2e-{run}-{attempt}-{job}-'):
        raise RuntimeError('Private target path ownership mismatch')
    if root.stat().st_uid != os.getuid() or root.stat().st_mode & 0o077 or state_path.stat().st_mode & 0o077: raise RuntimeError('Private target permissions invalid')
    state = json.loads(state_path.read_text())
    if state['identity'] != [run, attempt, job]: raise RuntimeError('Private target identity mismatch')
    for record in reversed(state['processes']):
        if not same_process(record):
            if Path(f'/proc/{record["pid"]}').exists(): raise RuntimeError('Process identity changed; refusing termination')
            continue
        os.killpg(record['pid'], signal.SIGTERM)
        deadline = time.monotonic() + 5
        while same_process(record) and time.monotonic() < deadline: time.sleep(0.1)
        if same_process(record): os.killpg(record['pid'], signal.SIGKILL)
    shutil.rmtree(root)
    print('Removed only this run-owned browser processes and private fixture files.')
    return 0

if __name__ == '__main__':
    try: raise SystemExit(main())
    except Exception:
        print('BLOCKED: browser cleanup ownership check failed; no unrelated process or directory was removed.', file=sys.stderr)
        raise SystemExit(2)
