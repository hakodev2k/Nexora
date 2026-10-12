import { useEffect, useRef, useState, type FormEvent } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { integrationCapabilities, integrationConnectors, integrationList, integrationGet, integrationSave, integrationDisable, type IntegrationConnector, type IntegrationConnection, type IntegrationConnectionCommand } from './integrationApi';

const emptyFilters = { query: '', provider: '', state: '' };
const emptyForm = (): IntegrationConnectionCommand => ({ schemaVersion: 1, providerKey: 'n8n', connectorVersion: 1, title: '', endpoint: '' });
const status = (e: unknown) => (e as { status?: number }).status;
const code = (e: unknown) => (e as { code?: string }).code;
const message = (e: unknown) => e instanceof Error ? e.message : 'Không hoàn tất thao tác.';
export function IntegrationConnectionsScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const generation = useRef(0), flight = useRef(false), pending = useRef<{ id: string | null; etag: string | null; key: string; body: IntegrationConnectionCommand | null } | null>(null), committed = useRef<string | null>(null);
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [connectors, setConnectors] = useState<IntegrationConnector[]>([]);
  const [items, setItems] = useState<IntegrationConnection[]>([]), [cursor, setCursor] = useState<string | null>(null);
  const [filters, setFilters] = useState(emptyFilters), [filterDraft, setFilterDraft] = useState(emptyFilters);
  const [busy, setBusy] = useState(false), [loading, setLoading] = useState(true), [error, setError] = useState(''), [formError, setFormError] = useState('');
  const [editor, setEditor] = useState<{ item: IntegrationConnection | null; baseline: string; disable: boolean } | null>(null), [form, setForm] = useState(emptyForm);
  const [uncertain, setUncertain] = useState(false), [conflict, setConflict] = useState<IntegrationConnection | null>(null), [confirmation, setConfirmation] = useState(false);
  const dirty = !!editor && (JSON.stringify(form) !== editor.baseline || uncertain);
  const can = (action: string) => caps['integrations.connection.' + action] === true;
  function denied(e: unknown) {
    if (status(e) !== 401 && status(e) !== 403 && code(e) !== 'ModuleUnavailable') return false;
    generation.current++; flight.current = false; setCaps({}); setConnectors([]); setItems([]); setCursor(null); setEditor(null); setForm(emptyForm()); pending.current = null; committed.current = null; setUncertain(false); setConflict(null); setBusy(false); setLoading(false); setError('Quyền hoặc module không còn khả dụng.');
    if (status(e) === 401) onAuthLost(); return true;
  }
  async function load(more = false) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setLoading(true); setError('');
    try {
      const allowed = await integrationCapabilities(), registry = await integrationConnectors(), page = await integrationList(filters, more ? cursor ?? undefined : undefined);
      if (stamp !== generation.current) return;
      setCaps(allowed); setConnectors(registry); setItems(old => more ? [...new Map([...old, ...page.items].map(x => [x.id, x])).values()] : page.items); setCursor(page.nextCursor);
    } catch (e) { if (stamp === generation.current && !denied(e)) { setError(message(e)); setCursor(null); if (status(e) === 404) setItems([]); } }
    finally { if (stamp === generation.current) { flight.current = false; setLoading(false); } }
  }
  useEffect(() => { generation.current++; flight.current = false; setItems([]); setCursor(null); void load(); return () => { generation.current++; }; }, [filters]);
  useEffect(() => registerDirtyLeaveGuard(() => { if (!dirty && !busy && !editor?.disable && !flight.current) return false; setFormError('Lưu hoặc hủy draft; nếu chưa rõ kết quả, retry cùng command trước khi rời.'); return true; }), [dirty, busy, editor]);
  useEffect(() => { const protect = (e: BeforeUnloadEvent) => { if (dirty || busy || editor?.disable || flight.current) { e.preventDefault(); e.returnValue = ''; } }; window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect); }, [dirty, busy, editor]);
  function begin(item: IntegrationConnection | null, disable = false) {
    const value = item ? { schemaVersion: 1 as const, providerKey: item.providerKey, connectorVersion: item.connectorVersion, title: item.title, endpoint: item.endpoint } : emptyForm();
    setForm(value); setEditor({ item, baseline: JSON.stringify(value), disable }); setFormError(''); setUncertain(false); setConflict(null); setConfirmation(false); pending.current = null; committed.current = null;
  }
  async function open(item: IntegrationConnection, disable = false) {
    if (flight.current || editor) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const current = await integrationGet(item.id); if (stamp === generation.current) begin(current, disable); }
    catch (e) { if (stamp === generation.current && !denied(e)) setError(message(e)); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  function close() { if (flight.current || uncertain) { setFormError('Retry cùng command trước khi đóng.'); return; } setEditor(null); setConflict(null); setForm(emptyForm()); setFormError(''); pending.current = null; committed.current = null; }
  async function save(event: FormEvent) {
    event.preventDefault(); if (!editor || flight.current) return;
    if (!pending.current) {
      if (editor.disable && !confirmation) { setFormError('Xác nhận disable connection.'); return; }
      if (!editor.disable && (!form.title.trim() || form.title.trim().length > 200 || !form.endpoint.trim())) { setFormError('Nhập tên1–200 ký tự và HTTPS service origin.'); return; }
      try { pending.current = { id: editor.item?.id ?? null, etag: editor.item?.etag ?? null, key: createIdempotencyKey(), body: editor.disable ? null : { ...form, title: form.title.trim(), endpoint: form.endpoint.trim() } }; }
      catch (e) { setFormError(message(e)); return; }
    }
    const command = pending.current, stamp = generation.current; flight.current = true; setBusy(true); setFormError('');
    try {
      const ack = command.body ? await integrationSave(command.id, command.etag, command.body, command.key) : await integrationDisable(command.id!, command.etag!, command.key);
      if (stamp !== generation.current) return;
      if (typeof ack?.id !== "string" || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(ack.id) || ack.id === "00000000-0000-0000-0000-000000000000" || (command.id && ack.id !== command.id)) throw Object.assign(new Error('ACK identity không hợp lệ.'), { status: 0 });
      committed.current = ack.id;
      const current = await integrationGet(ack.id); if (stamp !== generation.current) return;
      if (current.id !== ack.id) throw Object.assign(new Error('Current identity không hợp lệ.'), { status: 0 });
      pending.current = null; committed.current = null; setUncertain(false); setConflict(null); setEditor(null); setForm(emptyForm());
      flight.current = false; setBusy(false); await load();
    } catch (e) {
      if (stamp !== generation.current || denied(e)) return;
      const s = status(e), c = code(e);
      if (committed.current || s === undefined || s === 0 || s >= 500 || c === 'RequestInProgress' || c === 'ConcurrencyConflict') { setUncertain(true); setFormError('Chưa rõ kết quả; retry giữ nguyên body/ETag/key. ACK cũ không chứng minh state hiện tại.'); }
      else {
        pending.current = null; setUncertain(false);
        if (s === 412 && editor.item) {
          try { const current = await integrationGet(editor.item.id); if (stamp === generation.current) setConflict(current); }
          catch (readError) { if (stamp === generation.current && !denied(readError)) setFormError(message(readError)); }
        }
        if (stamp === generation.current) setFormError(message(e));
      }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <section className="module-workspace"><header><h2>Connections</h2><p>FX35 · Cấu hình inactive. Không network call, OAuth, credential, public endpoint hoặc delivery.</p></header>
    {error && <p role="alert">{error}</p>}
    <form onSubmit={e => { e.preventDefault(); if (!editor && !flight.current) setFilters({ ...filterDraft, query: filterDraft.query.trim() }); }}><fieldset disabled={busy || loading || !!editor}><legend>Lọc connections</legend><div className="form-grid"><label>Tên<input maxLength={200} value={filterDraft.query} onChange={e => setFilterDraft({ ...filterDraft, query: e.target.value })} /></label><label>Provider<select value={filterDraft.provider} onChange={e => setFilterDraft({ ...filterDraft, provider: e.target.value })}><option value="">Tất cả</option>{connectors.map(x => <option key={x.providerKey} value={x.providerKey}>{x.name}</option>)}</select></label><label>State<select value={filterDraft.state} onChange={e => setFilterDraft({ ...filterDraft, state: e.target.value })}><option value="">Tất cả</option><option>Disabled</option></select></label></div><button>Áp dụng</button><button type="button" onClick={() => { setFilterDraft(emptyFilters); setFilters({ ...emptyFilters }); }}>Xóa bộ lọc</button></fieldset></form>
    <button disabled={busy || loading || !!editor} onClick={() => void load()}>Tải lại</button>{can('create') && <button disabled={busy || loading || !!editor} onClick={() => begin(null)}>New connection</button>}
    {loading && <p role="status">Đang tải…</p>}{!loading && !error && items.length === 0 && <p>{filters.query || filters.provider || filters.state ? 'Không có connection khớp bộ lọc.' : 'Chưa có connection.'}</p>}
    {items.map(item => <article key={item.id}><h3>{item.title}</h3><p>{item.providerKey} v{item.connectorVersion} · {item.state} · Provider inactive</p><p>Auth: {item.authMode} · scopes chưa được duyệt · {item.lastTestAt ? 'Last test: ' + new Date(item.lastTestAt).toLocaleString() : 'Chưa thực hiện connection test'}</p><p>Cập nhật {new Date(item.updatedAt).toLocaleString()}</p><button disabled={busy || loading || !!editor} onClick={() => void open(item)}>Open</button>{can('disable') && <button disabled={busy || loading || !!editor} onClick={() => void open(item, true)}>Disable / invalidate authority</button>}</article>)}
    {cursor && !error && <button disabled={busy || loading || !!editor} onClick={() => void load(true)}>Tải thêm25 connections</button>}
    {editor && <ResourceFormDialog open title={editor.disable ? 'Disable connection' : editor.item ? 'Connection form' : 'New connection'} busy={busy} dirty={dirty} onClose={close}>
      <form onSubmit={save} noValidate>{formError && <p role="alert">{formError}</p>}
        <fieldset disabled={busy || uncertain || (editor.item !== null && !editor.disable && !can('update'))}><legend>{editor.disable ? 'Xác nhận connection' : 'Cấu hình'}</legend>
          {editor.disable ? <><p>{editor.item?.title}: connection tiếp tục Disabled và authority revision bị invalidated. Không xóa history, không thu hồi credential tại provider, không undo delivery đã hoàn thành.</p><label><input type="checkbox" checked={confirmation} onChange={e => setConfirmation(e.target.checked)} />Xác nhận disable</label></> : <><div className="form-grid"><label>Provider<select disabled={!!editor.item} value={form.providerKey} onChange={e => { const manifest = connectors.find(x => x.providerKey === e.target.value); if (manifest) setForm({ ...form, providerKey: manifest.providerKey, connectorVersion: manifest.version }); }}>{connectors.map(x => <option key={x.providerKey} value={x.providerKey}>{x.name} · config v{x.version}</option>)}</select></label><label>Tên<input maxLength={200} value={form.title} onChange={e => setForm({ ...form, title: e.target.value })} /></label><label>HTTPS service origin<input maxLength={2048} type="url" placeholder="https://service.example.com/" value={form.endpoint} onChange={e => setForm({ ...form, endpoint: e.target.value })} /></label></div><p>Chỉ origin HTTPS443, không path/query/credentials/IP literal. Đây là storage validation, không phải DNS/SSRF approval. Webhook URL cần contract riêng.</p><p>Auth chưa cấu hình; inbound commands/outbound events đều bị khóa. Không nhập token hoặc secret vào tên/origin.</p></>}
        </fieldset>
        {conflict && <section><h4>Revision đã thay đổi</h4><p>Current: {conflict.title} · {conflict.endpoint} · {conflict.state}</p><button type="button" disabled={busy} onClick={() => begin(conflict, editor.disable)}>Bỏ draft và tải current</button><button type="button" disabled={busy} onClick={() => { setEditor({ ...editor, item: conflict }); setConflict(null); setFormError('Đã chọn revision mới; kiểm tra draft trước khi Save.'); }}>Giữ draft trên revision mới</button></section>}
        {(uncertain || (editor.disable ? can('disable') : editor.item ? can('update') : can('create'))) && <button type="submit" disabled={busy || !!conflict || (editor.disable && !uncertain && !confirmation)}>{uncertain ? 'Retry cùng command' : editor.disable ? 'Xác nhận disable' : 'Save Disabled'}</button>}
        
      </form>
    </ResourceFormDialog>}
  </section>;
}
