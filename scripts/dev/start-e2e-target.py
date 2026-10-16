#!/usr/bin/env python3
"""Create a disposable, private, loopback browser target; never reuse an ambient target."""
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import uuid
import xml.etree.ElementTree as ET

REPO = Path(__file__).resolve().parents[2]
HARNESS_PHASE = 'validate isolated target'

def ci_identity():
    if os.environ.get('GITHUB_ACTIONS') != 'true' or os.environ.get('CI') != 'true':
        raise RuntimeError('GitHub Actions identity required')
    run, attempt, job = (os.environ.get(k, '') for k in ('GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'GITHUB_JOB'))
    if not re.fullmatch(r'[0-9]+', run) or not re.fullmatch(r'[0-9]+', attempt) or not re.fullmatch(r'[A-Za-z0-9_-]+', job):
        raise RuntimeError('Invalid CI identity')
    return run, attempt, job

def export_job(values):
    target = Path(os.environ.get('GITHUB_ENV', ''))
    if not target.is_file() or target.is_symlink(): raise RuntimeError('Trusted job environment required')
    with target.open('a') as stream:
        for key, value in values.items():
            if '\n' in str(value) or '\r' in str(value): raise RuntimeError('Invalid environment value')
            stream.write(f'{key}={value}\n')

def private_write(path, value):
    path.write_text(value); path.chmod(0o600)

def process_identity(pid):
    stat = Path(f'/proc/{pid}/stat').read_text().rsplit(')', 1)[1].split()
    command = Path(f'/proc/{pid}/cmdline').read_bytes()
    return {'pid': pid, 'start': stat[19], 'commandHash': hashlib.sha256(command).hexdigest()}

def sanitize_junit(path):
    if not path.exists(): return
    tree = ET.parse(path)
    for node in tree.iter():
        for child in list(node):
            if child.tag in ('system-out', 'system-err', 'properties', 'attachments'): node.remove(child)
        if node.tag in ('failure', 'error', 'skipped'):
            node.clear(); node.text = 'Browser assertion failed. Secret-bearing call logs are excluded from uploaded evidence.'
    tree.write(path, encoding='utf-8', xml_declaration=True)

def main():
    global HARNESS_PHASE
    run, attempt, job = ci_identity()
    if sys.argv[1:] == ['run-browser']:
        HARNESS_PHASE = 'run browser assertions'
        state_path = Path(os.environ.get('NEXORA_E2E_TARGET_STATE', ''))
        state = json.loads(state_path.read_text())
        if state['identity'] != [run, attempt, job]: raise RuntimeError('Target ownership mismatch')
        root = state_path.parent
        env = json.loads((root / 'environment.json').read_text())
        with (root / 'browser-private.log').open('wb') as log:
            result = subprocess.run(['npm', 'run', 'test:e2e', '--prefix', str(REPO / 'web/Nexora.Web')], cwd=REPO, env=env, stdout=log, stderr=log)
        report = REPO / 'web/Nexora.Web/artifacts/playwright-junit.xml'
        sanitize_junit(report)
        print('Browser tests completed; sanitized JUnit records exact outcomes. Raw browser call logs remain private.')
        return result.returncode
    if sys.argv[1:] == ['sanitize-evidence']:
        sanitize_junit(REPO / 'web/Nexora.Web/artifacts/playwright-junit.xml')
        sanitize_junit(REPO / 'web/Nexora.Web/artifacts/functional-junit.xml')
        return 0
    if sys.argv[1:]: raise RuntimeError('Use without arguments, run-browser, or sanitize-evidence')
    connection = os.environ.get('NEXORA_TEST_SQL_CONNECTION', '')
    if not re.fullmatch(r'Server=127\.0\.0\.1,14339;Database=Nexora_Test_[0-9a-f]{32};User Id=sa;Password=Nx!7[0-9a-f]{48};Encrypt=True;TrustServerCertificate=True;Persist Security Info=False', connection):
        raise RuntimeError('Fresh CI-owned SQL connection required')
    os.umask(0o077)
    runner_temp = Path(os.environ['RUNNER_TEMP']).resolve()
    root = Path(tempfile.mkdtemp(prefix=f'nexora-ci-e2e-{run}-{attempt}-{job}-', dir=runner_temp))
    state_path = root / 'state.json'
    state = {'identity': [run, attempt, job], 'processes': []}
    private_write(state_path, json.dumps(state))
    export_job({'NEXORA_E2E_TARGET_STATE': str(state_path)})
    # A copied child environment is the only environment mutated. Remove all
    # inherited app/local path/config overrides before installing this target.
    env = {key: value for key, value in os.environ.items() if not key.lower().startswith(('nexora', 'connectionstrings', 'aspnetcore'))}
    for name in ('capture', 'files', 'keys'): (root / name).mkdir(mode=0o700)
    database = 'Nexora_Test_' + uuid.uuid4().hex
    connection = re.sub(r'Database=Nexora_Test_[0-9a-f]{32}', 'Database=' + database, connection)
    print('::add-mask::' + connection)
    with socket.socket() as listener:
        listener.bind(('127.0.0.1', 0)); api_port = listener.getsockname()[1]
    api_url = f'http://127.0.0.1:{api_port}'
    run_id = f'nexora-e2e-{run}-{attempt}-{uuid.uuid4().hex[:8]}'
    env.update({
        'DOTNET_ENVIRONMENT': 'Development', 'ASPNETCORE_ENVIRONMENT': 'Development',
        'ASPNETCORE_URLS': api_url, 'ASPNETCORE_CONTENTROOT': str(REPO / 'src/Nexora.Api'),
        'NEXORA_SQL_CONNECTION_STRING': connection, 'NEXORA_SQL_CONNECTION': connection,
        'ConnectionStrings__NexoraSql': connection,
        'NEXORA_MIGRATIONS_DIR': str(REPO / 'database/migrations'),
        'NEXORA_LOCAL_MESSAGE_CAPTURE_PATH': str(root / 'capture'),
        'Nexora__LocalAccountMessageCapturePath': str(root / 'capture'),
        'NEXORA_LOCAL_FILE_STORAGE_PATH': str(root / 'files'), 'Nexora__LocalFileStoragePath': str(root / 'files'),
        'NEXORA_ALLOWED_ORIGINS': 'http://localhost:5173,http://127.0.0.1:5173',
        'NEXORA_E2E_BASE_URL': 'http://localhost:5173', 'NEXORA_E2E_RUN_ID': run_id,
        'NEXORA_E2E_ACCOUNTS': str(root / 'accounts.json'), 'NEXORA_E2E_LOGIN_PACING_MS': '6500',
        'NEXORA_E2E_TARGET_STATE': str(state_path),
        'Nexora__RuntimeMode': 'LocalOnly',
        'ASPNETCORE_Logging__LogLevel__Default': 'Warning',
    })
    for name in ('NEXORA_IDEMPOTENCY_SECRET', 'NEXORA_CSRF_SECRET', 'NEXORA_LOCAL_MESSAGE_KEY', 'NEXORA_MODULE_PREVIEW_SECRET'):
        value = secrets.token_urlsafe(48); env[name] = value; print('::add-mask::' + value)
    dotnet = shutil_which('dotnet')
    local_dll = REPO / 'src/Nexora.Local/bin/Release/net10.0/Nexora.Local.dll'
    sql_dll = REPO / 'tests/Nexora.FunctionalTestOperator/bin/Release/net10.0/Nexora.FunctionalTestOperator.dll'
    api_dll = REPO / 'src/Nexora.Api/bin/Release/net10.0/Nexora.Api.dll'
    for path in (local_dll, sql_dll, api_dll):
        if not path.is_file(): raise RuntimeError('Required harness executable must be built first')
    env_path = root / 'environment.json'
    wrapper_template = '''#!/usr/bin/env python3
import json, os, subprocess, sys
from pathlib import Path
env=json.loads(Path(ENV_PATH).read_text())
raise SystemExit(subprocess.call([DOTNET, DLL, *sys.argv[1:]],env=env))
'''
    for name, dll in (('account-operator', local_dll), ('sql-operator', sql_dll)):
        wrapper = root / name
        private_write(wrapper, wrapper_template.replace('ENV_PATH', repr(str(env_path))).replace('DOTNET', repr(dotnet)).replace('DLL', repr(str(dll))))
        wrapper.chmod(0o700)
    env['NEXORA_E2E_OPERATOR_CLI'] = str(root / 'account-operator'); env['NEXORA_E2E_SQL_OPERATOR'] = str(root / 'sql-operator')
    private_write(env_path, json.dumps(env))
    HARNESS_PHASE = 'seed approved synthetic SQL fixtures'
    with (root / 'seed-private.log').open('wb') as log:
        subprocess.run([dotnet, str(sql_dll), 'seed'], cwd=REPO, env=env, stdout=log, stderr=log, check=True)
    (root / 'accounts.json').chmod(0o600)
    for account in json.loads((root / 'accounts.json').read_text()): print('::add-mask::' + account['password'])
    vite_root = REPO / 'web/Nexora.Web'
    vite_config = root / 'vite.config.mjs'
    private_write(vite_config, 'import {defineConfig} from ' + json.dumps((vite_root / 'node_modules/vite/dist/node/index.js').as_uri()) + ';\nimport react from ' + json.dumps((vite_root / 'node_modules/@vitejs/plugin-react/dist/index.js').as_uri()) + ';\nexport default defineConfig({root:' + json.dumps(str(vite_root)) + ',plugins:[react()],server:{host:"127.0.0.1",port:5173,strictPort:true,proxy:{"/api":{target:' + json.dumps(api_url) + ',changeOrigin:true},"/health":{target:' + json.dumps(api_url) + ',changeOrigin:true}}}});\n')
    HARNESS_PHASE = 'start owned API and Vite processes'
    try:
        for name, command, cwd in (
            ('api', [dotnet, str(api_dll)], REPO / 'src/Nexora.Api'),
            ('vite', [shutil_which('node'), str(vite_root / 'node_modules/vite/bin/vite.js'), '--config', str(vite_config)], vite_root),
        ):
            with (root / (name + '-private.log')).open('wb') as log:
                process = subprocess.Popen(command, cwd=cwd, env=env, stdout=log, stderr=log, start_new_session=True)
            time.sleep(0.1)
            state['processes'].append(process_identity(process.pid)); private_write(state_path, json.dumps(state))
        HARNESS_PHASE = 'verify real API and proxied SQL readiness'
        for url in (api_url + '/health/ready', 'http://127.0.0.1:5173/health/ready'):
            deadline = time.monotonic() + 60
            while True:
                if any(not Path(f'/proc/{p["pid"]}').exists() for p in state['processes']): raise RuntimeError('Owned target process exited')
                try:
                    with urllib.request.urlopen(url, timeout=2) as response:
                        if response.status == 200: break
                except (OSError, urllib.error.URLError): pass
                if time.monotonic() > deadline: raise RuntimeError('Real API/SQL readiness failed')
                time.sleep(0.5)
    except Exception:
        subprocess.run([sys.executable, str(REPO / 'scripts/dev/stop-e2e-target.py')], env={**os.environ, 'NEXORA_E2E_TARGET_STATE': str(state_path)}, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        raise
    export_job({key: env[key] for key in ('NEXORA_E2E_BASE_URL', 'NEXORA_E2E_RUN_ID', 'NEXORA_E2E_ACCOUNTS', 'NEXORA_E2E_OPERATOR_CLI', 'NEXORA_E2E_SQL_OPERATOR', 'NEXORA_E2E_TARGET_STATE')})
    print('Private synthetic loopback browser target ready (SQL/API readiness verified).')
    return 0

def shutil_which(name):
    import shutil
    result = shutil.which(name)
    if not result: raise RuntimeError('Required executable unavailable: ' + name)
    return result

if __name__ == '__main__':
    try: raise SystemExit(main())
    except Exception:
        # Provider exceptions and operator output may carry credentials/tokens.
        print('BLOCKED: ' + HARNESS_PHASE + ' failed; no secret-bearing provider or browser output was published.', file=sys.stderr)
        raise SystemExit(2)
