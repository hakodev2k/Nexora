import { useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { getShareLink, updateShareLink, type ShareLinkRecord } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';

type Draft = { mode: string; expiry: string; noExpiry: boolean; audience: string };
const draftOf = (item: ShareLinkRecord): Draft => ({ mode: item.mode, expiry: item.expiresAt?.slice(0, 19) ?? '', noExpiry: item.expiresAt === null, audience: item.allowedUserIds.join(', ') });
const denied = (error: unknown) => {
  const failure = error as { status?: number; code?: string };
  return [401, 403, 404].includes(failure.status ?? -1) || failure.code === 'ModuleUnavailable';
};

export function ShareLinkEditor({ item, onUpdated, onClose, onDenied }: {
  item: ShareLinkRecord; onUpdated: (item: ShareLinkRecord) => void; onClose: () => void; onDenied: () => void;
}) {
  const prefix = useId();
  const [current, setCurrent] = useState(item), [draft, setDraft] = useState(draftOf(item));
  const [busy, setBusy] = useState(false), [ready, setReady] = useState(false), [error, setError] = useState('');
  const [uncertain, setUncertain] = useState(false), [conflict, setConflict] = useState<ShareLinkRecord | null>(null);
  const mounted = useRef(false), flight = useRef(false), generation = useRef(0);
  const request = useRef<{ etag: string; mode: string; expiresAt: string | null; users: string[]; noExpiry: boolean; key: string } | null>(null);
  const dirty = JSON.stringify(draft) !== JSON.stringify(draftOf(current));
  useEffect(() => {
    mounted.current = true; const stamp = ++generation.current;
    void getShareLink(item.id).then(value => {
      if (!mounted.current || stamp !== generation.current) return;
      if (value.revokedAt !== null) { onDenied(); return; }
      setCurrent(value); setDraft(draftOf(value)); setReady(true);
    }).catch(failure => { if (mounted.current && stamp === generation.current) { if (denied(failure)) onDenied(); else setError('Không thể tải phiên bản link hiện tại. Đóng và mở lại để thử.'); } });
    return () => { mounted.current = false; generation.current++; };
  }, [item.id]);
  useEffect(() => registerDirtyLeaveGuard(() => {
    if (!dirty && !busy && !uncertain) return false;
    setError('Hãy lưu hoặc hủy bản nháp trong dialog trước khi rời màn hình.'); return true;
  }), [dirty, busy, uncertain]);
  useEffect(() => {
    const protect = (event: BeforeUnloadEvent) => { if (dirty || busy || uncertain) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect);
  }, [dirty, busy, uncertain]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (flight.current || !ready || conflict) return;
    setError('');
    if (!request.current) {
      const users = draft.mode === 'RestrictedUsers' ? [...new Set(draft.audience.split(',').map(value => value.trim()).filter(Boolean))] : [];
      if (draft.mode === 'RestrictedUsers' && (users.length === 0 || users.some(value => !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) || value === '00000000-0000-0000-0000-000000000000'))) {
        setError('Restricted users cần ít nhất một verified user ID hợp lệ.'); return;
      }
      let expiresAt: string | null = null;
      if (!draft.noExpiry && draft.expiry) {
        const date = new Date(draft.expiry + 'Z');
        if (!Number.isFinite(date.getTime()) || date.getTime() <= Date.now()) { setError('Hết hạn phải là thời điểm UTC trong tương lai.'); return; }
        expiresAt = draft.expiry === draftOf(current).expiry ? current.expiresAt : date.toISOString();
      }
      request.current = { etag: current.etag, mode: draft.mode, expiresAt, users, noExpiry: draft.noExpiry, key: crypto.randomUUID() };
    }
    const stamp = generation.current; const command = request.current; flight.current = true; setBusy(true);
    try {
      const updated = await updateShareLink(item.id, command.etag, command.mode, command.expiresAt, command.users, command.noExpiry, command.key);
      if (!mounted.current || stamp !== generation.current) return;
      if (!updated || updated.id !== item.id || updated.resourceId !== item.resourceId || updated.resourceType !== item.resourceType ||
          typeof updated.etag !== 'string' || !/^"[A-Za-z0-9+/]{11}="$/.test(updated.etag) || !Array.isArray(updated.allowedUserIds))
        throw Object.assign(new Error('Missing save acknowledgement'), { status: 0 });
      const version = atob(updated.etag.slice(1, -1));
      if (version.length !== 8 || '"' + btoa(version) + '"' !== updated.etag ||
          !['PublicLink', 'AuthenticatedLink', 'RestrictedUsers'].includes(updated.mode) || typeof updated.isActive !== 'boolean')
        throw Object.assign(new Error('Invalid save acknowledgement'), { status: 0 });
      onUpdated({ ...updated, token: null });
    } catch (failure) {
      if (!mounted.current || stamp !== generation.current) return;
      const status = (failure as { status?: number }).status, code = (failure as { code?: string }).code;
      if (denied(failure)) { onDenied(); return; }
      if (status === 412) {
        request.current = null; setUncertain(false);
        try { const value = await getShareLink(item.id); if (mounted.current && stamp === generation.current) { setConflict(value); setError('Link đã thay đổi. So sánh bản hiện tại trước khi áp dụng lại bản nháp.'); } }
        catch (readFailure) { if (mounted.current && stamp === generation.current) { if (denied(readFailure)) onDenied(); else { setReady(false); setError('Không thể tải bản hiện tại. Đóng và mở lại để thử.'); } } }
      } else if (status === undefined || status === 0 || status === 503 || code === 'RequestInProgress') {
        setUncertain(true); setError('Chưa xác định kết quả lưu. Retry giữ nguyên nội dung, ETag và Idempotency-Key.');
      } else {
        request.current = null; setUncertain(false); setError(failure instanceof Error ? failure.message : 'Không thể lưu link.');
      }
    } finally { if (mounted.current && stamp === generation.current) { flight.current = false; setBusy(false); } }
  }

  async function reapply() {
    if (!conflict || flight.current) return;
    const stamp = generation.current; const compared = conflict; flight.current = true; setBusy(true);
    try {
      const latest = await getShareLink(item.id); if (!mounted.current || stamp !== generation.current) return;
      if (latest.revokedAt !== null) { onDenied(); return; }
      if (latest.etag !== compared.etag) { setConflict(latest); setError('Bản hiện tại lại thay đổi. Hãy so sánh lại.'); return; }
      setCurrent(latest); setConflict(null); request.current = null; setError('Bản nháp được giữ lại. Bấm lưu để gửi với ETag mới.');
    } catch (failure) { if (mounted.current && stamp === generation.current) { if (denied(failure)) onDenied(); else setError('Không thể kiểm tra phiên bản hiện tại.'); } }
    finally { if (mounted.current && stamp === generation.current) { flight.current = false; setBusy(false); } }
  }

  return <ResourceFormDialog open title="Sửa link chia sẻ" busy={busy} dirty={dirty || uncertain} onClose={onClose}>
    <p>Resource cố định: {item.resourceType} · {item.resourceId}. URL và token không thay đổi khi sửa.</p>
    {error && <p role="alert">{error}</p>}
    {!ready && !error && <p role="status">Đang tải phiên bản hiện tại…</p>}
    {conflict && <section aria-label="Phiên bản link hiện tại"><p>{conflict.mode} · {conflict.expiresAt ?? 'Không hết hạn'}</p><p>Allowed users: {conflict.allowedUserIds.join(', ') || 'Không có'}</p><button type="button" className="secondary-button" onClick={() => void reapply()}>Áp dụng lại bản nháp</button></section>}
    <form onSubmit={save} noValidate><fieldset className="resource-dialog-fields" disabled={!ready || uncertain || busy || conflict !== null}>
      <div className="field-group"><label htmlFor={prefix + '-mode'}>Audience</label><select id={prefix + '-mode'} value={draft.mode} onChange={event => { request.current = null; setDraft({ ...draft, mode: event.target.value }); }}><option value="PublicLink">Public link</option><option value="AuthenticatedLink">Authenticated account</option><option value="RestrictedUsers">Restricted users</option></select></div>
      <label className="field-group">Hết hạn (UTC)<input type="datetime-local" step="1" disabled={draft.noExpiry} value={draft.expiry} onChange={event => { request.current = null; setDraft({ ...draft, expiry: event.target.value }); }} /></label>
      <label className="checkbox-row"><input type="checkbox" checked={draft.noExpiry} onChange={event => { request.current = null; setDraft({ ...draft, noExpiry: event.target.checked }); }} />Không hết hạn</label>
      {draft.mode === 'RestrictedUsers' && <label className="field-group">Verified user IDs<input value={draft.audience} onChange={event => { request.current = null; setDraft({ ...draft, audience: event.target.value }); }} /></label>}
    </fieldset><button className="primary-button" type="submit" disabled={!ready || busy || conflict !== null}>{busy ? 'Đang lưu…' : uncertain ? 'Retry cùng request' : 'Lưu link'}</button></form>
  </ResourceFormDialog>;
}
