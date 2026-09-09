import { useEffect, useMemo, useState } from 'react';
import {
  AdminModuleResponse,
  DevAccountMessage,
  ProfileResponse,
  getCsrf,
  getMe,
  listAdminModulesDev,
  listDevAccountMessages,
  listSessions,
  login,
  logout,
  registerDemoUser,
  updateMe,
  verifyEmail
} from './api';

const demoPassword = 'correct-horse-phrase';

export function App() {
  const [status, setStatus] = useState<'checking' | 'ready' | 'unavailable'>('checking');
  const [message, setMessage] = useState('Đang kiểm tra Nexora local API...');
  const [log, setLog] = useState<string[]>([]);
  const [messages, setMessages] = useState<DevAccountMessage[]>([]);
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [modules, setModules] = useState<AdminModuleResponse[]>([]);
  const demoEmail = useMemo(() => `demo-${Date.now()}@example.test`, []);

  useEffect(() => {
    let cancelled = false;
    getCsrf()
      .then((token) => {
        if (!cancelled) {
          setStatus('ready');
          setMessage(`M01 API surface sẵn sàng. CSRF token đang giữ trong memory (${token.tokenType}).`);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setStatus('unavailable');
          setMessage('Backend local chưa chạy hoặc endpoint M01 chưa sẵn sàng.');
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  async function run(label: string, action: () => Promise<unknown>) {
    try {
      const result = await action();
      setLog((items) => [`[ok] ${label}: ${JSON.stringify(result, null, 2)}`, ...items]);
    } catch (error) {
      setLog((items) => [`[error] ${label}: ${error instanceof Error ? error.message : String(error)}`, ...items]);
    }
  }

  async function refreshMessages() {
    const result = await listDevAccountMessages();
    setMessages(result);
    return result;
  }

  async function verifyLatestMessage() {
    const currentMessages = messages.length > 0 ? messages : await listDevAccountMessages();
    const verification = currentMessages.find((item) => item.purpose === 'EmailVerification');
    if (!verification) {
      throw new Error('No captured EmailVerification message found. Register first.');
    }

    const result = await verifyEmail(verification.token);
    setProfile(result.profile);
    return result;
  }

  async function loginDemoUser() {
    const result = await login(demoEmail, demoPassword);
    setProfile(result.profile);
    return result;
  }

  async function loadProfile() {
    const result = await getMe();
    setProfile(result);
    return result;
  }

  async function updateDisplayName() {
    const result = await updateMe({ displayName: 'Nexora Demo User' });
    setProfile(result);
    return result;
  }

  async function loadModules() {
    const result = await listAdminModulesDev();
    setModules(result.items);
    return result;
  }

  return (
    <main className="shell" aria-labelledby="page-title">
      <section className="card">
        <p className="eyebrow">Nexora</p>
        <h1 id="page-title">M01 minimal API implementation shell</h1>
        <p>{message}</p>
        <dl>
          <dt>Approved decision</dt>
          <dd>DEC-20260909-001</dd>
          <dt>Current runtime surface</dt>
          <dd>S02–S06 identity APIs and S07/S09 module catalog smoke APIs, all development-only until SQL-backed.</dd>
          <dt>Status</dt>
          <dd>{status}</dd>
          <dt>Demo email</dt>
          <dd>{demoEmail}</dd>
        </dl>
      </section>

      <section className="card" aria-labelledby="demo-title">
        <h2 id="demo-title">Local identity flow</h2>
        <p className="hint">
          This screen drives the real local API routes. The dev mailbox endpoint is available only in Development so the email verification token can be copied without sending real email.
        </p>
        <div className="actions">
          <button onClick={() => run('register', () => registerDemoUser(demoEmail, demoPassword))}>Register</button>
          <button onClick={() => run('dev mailbox', refreshMessages)}>Load dev mailbox</button>
          <button onClick={() => run('verify latest email', verifyLatestMessage)}>Verify latest email</button>
          <button onClick={() => run('login', loginDemoUser)}>Login</button>
          <button onClick={() => run('get me', loadProfile)}>Get me</button>
          <button onClick={() => run('update display name', updateDisplayName)}>Update display name</button>
          <button onClick={() => run('list sessions', listSessions)}>List sessions</button>
          <button onClick={() => run('logout', logout)}>Logout</button>
        </div>
      </section>

      <section className="card" aria-labelledby="module-title">
        <h2 id="module-title">Local module catalog smoke flow</h2>
        <p className="hint">
          Uses an explicit development-only SuperAdmin proof header. This is not the final authorization model; the SQL-backed access layer must replace it before M01 is verified.
        </p>
        <div className="actions">
          <button onClick={() => run('list admin modules', loadModules)}>List admin modules</button>
        </div>
        {modules.length > 0 && (
          <ul className="module-list" aria-label="Module catalog">
            {modules.map((module) => (
              <li key={module.id}>
                <strong>{module.code}</strong> — {module.name}: {module.state}, system={String(module.systemEnabled)}, registration={String(module.registrationEnabled)}
                {module.unavailableReason ? ` (${module.unavailableReason})` : ''}
              </li>
            ))}
          </ul>
        )}
      </section>

      {profile && (
        <section className="card" aria-labelledby="profile-title">
          <h2 id="profile-title">Current profile</h2>
          <pre>{JSON.stringify(profile, null, 2)}</pre>
        </section>
      )}

      <section className="card" aria-labelledby="log-title">
        <h2 id="log-title">Runtime log</h2>
        <pre>{log.join('\n\n') || 'No API calls yet.'}</pre>
      </section>
    </main>
  );
}
