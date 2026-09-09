import { useEffect, useState } from 'react';
import { getCsrf } from './api';

export function App() {
  const [status, setStatus] = useState<'checking' | 'ready' | 'unavailable'>('checking');
  const [message, setMessage] = useState('Đang kiểm tra Nexora local scaffold...');

  useEffect(() => {
    let cancelled = false;
    getCsrf()
      .then((token) => {
        if (!cancelled) {
          setStatus('ready');
          setMessage(`M01-S00 scaffold sẵn sàng. CSRF token đang giữ trong memory (${token.tokenType}).`);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setStatus('unavailable');
          setMessage('Backend local chưa chạy hoặc endpoint M01-S00 chưa sẵn sàng.');
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <main className="shell" aria-labelledby="page-title">
      <section className="card">
        <p className="eyebrow">Nexora</p>
        <h1 id="page-title">M01 local implementation scaffold</h1>
        <p>{message}</p>
        <dl>
          <dt>Approved decision</dt>
          <dd>DEC-20260909-001</dd>
          <dt>Current slice</dt>
          <dd>M01-S00</dd>
          <dt>Status</dt>
          <dd>{status}</dd>
        </dl>
      </section>
    </main>
  );
}
