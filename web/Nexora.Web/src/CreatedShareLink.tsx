import { useEffect, useRef, useState } from 'react';
import { apiFetch } from './api';

export function CreatedShareLink({ id, token, onUnavailable }: { id: string; token: string; onUnavailable: () => void }) {
  const [status, setStatus] = useState('');
  const [busy, setBusy] = useState(false);
  const generation = useRef(0);
  const flight = useRef(false);
  const url = new URL('/share/' + encodeURIComponent(token), window.location.origin).href;
  useEffect(() => {
    generation.current++; flight.current = false; setBusy(false); setStatus('');
    return () => { generation.current++; };
  }, [id, token]);

  async function copy() {
    if (flight.current) return;
    flight.current = true; setBusy(true); setStatus('');
    const stamp = generation.current;
    try {
      const capability = await apiFetch<{ allowed: boolean }>('/api/v1/sharing/links/' + encodeURIComponent(id) + '/copy-capability');
      if (stamp !== generation.current) return;
      if (capability?.allowed !== true) { onUnavailable(); return; }
      if (!navigator.clipboard?.writeText) {
        setStatus('Clipboard không khả dụng. Chọn URL bên dưới để copy thủ công.'); return;
      }
      await navigator.clipboard.writeText(url);
      if (stamp === generation.current) setStatus('Đã copy URL.');
    } catch (error) {
      if (stamp !== generation.current) return;
      const failure = error as { status: number; code?: string };
      if ([401, 403, 404].includes(failure.status) || failure.code === 'ModuleUnavailable') { onUnavailable(); return; }
      setStatus('Không thể copy URL. Kiểm tra quyền clipboard rồi thử lại.');
    } finally {
      if (stamp === generation.current) { flight.current = false; setBusy(false); }
    }
  }

  return <div className="success-panel"><h2>Link đã được tạo</h2>
    <p>URL chỉ có trong kết quả tạo hiện tại. Hãy copy trước khi tải lại hoặc rời màn hình.</p>
    <label className="field-group">URL vừa tạo<input className="secret-output" readOnly value={url} onFocus={event => event.currentTarget.select()} /></label>
    <button className="secondary-button" type="button" disabled={busy} onClick={() => void copy()}>{busy ? 'Đang kiểm tra…' : 'Copy URL'}</button>
    <a href={url} rel="noreferrer" referrerPolicy="no-referrer">Mở projection read-only</a>
    <p role="status">{status}</p>
  </div>;
}
