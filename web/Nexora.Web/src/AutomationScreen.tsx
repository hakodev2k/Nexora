import { useEffect, useRef, useState, type FormEvent } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { createIdempotencyKey } from './api';
import { AutomationLifecycleDialog } from './AutomationLifecycleDialog';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationCapabilities, automationGet, automationHistory, automationList, automationSave, automationValidate, automationImport, automationExport,
  type AutomationCommand, type AutomationDefinition, type AutomationDetail, type AutomationShape, type AutomationStep, type AutomationVersion } from './automationApi';

const emptyStep = (id: number): AutomationStep => ({ id, action: { key: 'tasks.task.create', version: 1 }, inputs: { projectId: '', title: '', description: null, startAt: '', endAt: '', priority: 'P2' } });
const emptyShape = (): AutomationShape => ({ schemaVersion: 1, trigger: { key: 'manual', version: 1 }, steps: [emptyStep(1)] });
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';
const status = (e: unknown) => (e as { status?: number }).status;
const code = (e: unknown) => (e as { code?: string }).code;
const instant = (value: string) => /\.\d{7}(?:Z|[+-]\d{2}:\d{2})$/.test(value) ? value : new Date(value).toISOString().replace('Z', '0000+00:00');
const localTime = (value: string) => {
  const date = new Date(value); if (!Number.isFinite(date.getTime())) throw new Error('Thời gian lưu không hợp lệ.');
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};
function editShape(detail: AutomationDetail): AutomationShape {
  const shape = JSON.parse(detail.version.definitionJson) as AutomationShape;
  if (shape.schemaVersion !== 1 || shape.trigger.key !== 'manual' || !Array.isArray(shape.steps)) throw new Error('Version chưa được editor hỗ trợ.');
  return shape;
}

export function AutomationScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const generation = useRef(0), flight = useRef(false);
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [items, setItems] = useState<AutomationDefinition[]>([]);
  const [lifecycle, setLifecycle] = useState('Active'), [lifecycleItem, setLifecycleItem] = useState<AutomationDefinition | null>(null);
  const [query, setQuery] = useState(''), [filter, setFilter] = useState(''), [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [error, setError] = useState('');
  const [editor, setEditor] = useState<{ id: string | null; etag: string | null; baseline: string; importing?: boolean } | null>(null);
  const [title, setTitle] = useState(''), [shape, setShape] = useState<AutomationShape>(emptyShape);
  const [formError, setFormError] = useState(''), [uncertain, setUncertain] = useState(false), [conflict, setConflict] = useState<AutomationDetail | null>(null);
  const [versions, setVersions] = useState<{ id: string; items: AutomationVersion[]; next: number | null } | null>(null);
  const pending = useRef<{ id: string | null; etag: string | null; body: AutomationCommand; key: string } | null>(null);
  const knownAck = useRef<{ id: string; versionNumber: number } | null>(null);
  const draft = JSON.stringify({ title, shape }), dirty = editor !== null && (editor.baseline !== draft || uncertain);
  const can = (action: string) => caps['automation.definition.' + action] === true;
  function denied(e: unknown) {
    if (status(e) !== 401 && status(e) !== 403 && code(e) !== 'ModuleUnavailable') return false;
    generation.current++; flight.current = false; setCaps({}); setLifecycleItem(null); setItems([]); setVersions(null); setEditor(null); setTitle(''); setShape(emptyShape());
    setCursor(null); pending.current = null; knownAck.current = null; setUncertain(false); setConflict(null); setLoading(false); setBusy(false); setError('Quyền hoặc module không còn khả dụng.');
    if (status(e) === 401) onAuthLost(); return true;
  }
  async function load(more = false) {
    if (flight.current) return;
    const stamp = generation.current; flight.current = true; setLoading(true); setError('');
    try {
      const allowed = await automationCapabilities();
      const page = await automationList(filter, more ? cursor ?? undefined : undefined, lifecycle);
      if (stamp !== generation.current) return;
      setCaps(allowed); setItems(current => more ? [...new Map([...current, ...page.items].map(item => [item.id, item])).values()] : page.items); setCursor(page.nextCursor);
    } catch (e) { if (stamp === generation.current && !denied(e)) { setError(message(e)); setCursor(null); } }
    finally { if (stamp === generation.current) { flight.current = false; setLoading(false); } }
  }
  useEffect(() => { ++generation.current; flight.current = false; void load(); return () => { generation.current++; }; }, [filter, lifecycle]);
  useEffect(() => registerDirtyLeaveGuard(() => { if (!dirty && !busy && !lifecycleItem) return false; setFormError('Lưu hoặc hủy bản nháp trước khi rời trang. Retry giữ nguyên command khi chưa rõ kết quả.'); return true; }), [dirty, busy, lifecycleItem]);
  useEffect(() => { const protect = (e: BeforeUnloadEvent) => { if (dirty || busy || lifecycleItem) { e.preventDefault(); e.returnValue = ''; } }; window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect); }, [dirty, busy, lifecycleItem]);
  function begin(detail?: AutomationDetail) {
    if (editor || uncertain || pending.current) return;
    const value = detail ? editShape(detail) : emptyShape(), name = detail?.definition.title ?? '';
    setShape(value); setTitle(name); setEditor({ id: detail?.definition.id ?? null, etag: detail?.definition.etag ?? null, baseline: JSON.stringify({ title: name, shape: value }) });
    pending.current = null; knownAck.current = null; setUncertain(false); setConflict(null); setFormError('');
  }
  async function open(item: AutomationDefinition) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true);
    try { const detail = await automationGet(item.id); if (stamp === generation.current) begin(detail); }
    catch (e) { if (stamp === generation.current && !denied(e)) setError(message(e)); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  function body(): AutomationCommand {
    if (!title.trim() || title.trim().length > 200) throw new Error('Tên workflow cần 1–200 ký tự.');
    return { title: title.trim(), definitionJson: JSON.stringify({ ...shape, steps: shape.steps.map(step => ({ ...step, inputs: { ...step.inputs, startAt: instant(step.inputs.startAt), endAt: instant(step.inputs.endAt) } })) }) };
  }
  async function validate() {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setFormError('');
    try { const result = await automationValidate(body()); if (stamp === generation.current) setFormError(result.shapeAccepted && result.referencesValid ? 'Cấu trúc và Project đích hiện hợp lệ. Execution và simulation chưa khả dụng.' : result.code); }
    catch (e) { if (stamp === generation.current && !denied(e)) setFormError(message(e)); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function save(e: FormEvent) {
    e.preventDefault(); if (!editor || flight.current || conflict) return;
    const stamp = generation.current; flight.current = true; setBusy(true); setFormError('');
    try {
      if (!pending.current) pending.current = { id: editor.id, etag: editor.etag, body: body(), key: createIdempotencyKey() };
      const command = pending.current, ack = editor.importing ? await automationImport(command.body, command.key) : await automationSave(command.id, command.etag, command.body, command.key);
      if (stamp !== generation.current) return;
      if (!ack || typeof ack.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(ack.id) || ack.id === '00000000-0000-0000-0000-000000000000' || typeof ack.etag !== 'string' || !/^"[A-Za-z0-9+/]{11}="$/.test(ack.etag) || !Number.isSafeInteger(ack.versionNumber) || ack.versionNumber < 1 || (command.id && ack.id !== command.id)) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      const bytes = atob(ack.etag.slice(1, -1));
      if (bytes.length !== 8 || '"' + btoa(bytes) + '"' !== ack.etag) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      knownAck.current = ack;
      const current = await automationGet(ack.id); if (stamp !== generation.current) return;
      if (current.definition.currentVersion !== ack.versionNumber || current.definition.etag !== ack.etag) {
        pending.current = null; knownAck.current = null; setUncertain(false); setEditor({ ...editor, id: ack.id, importing: false }); setConflict(current);
        setFormError(`Đã lưu version ${ack.versionNumber}; workflow hiện ở version ${current.definition.currentVersion}. Bản nháp được giữ để so sánh.`); return;
      }
      pending.current = null; knownAck.current = null; setUncertain(false); setEditor(null); flight.current = false; await load();
    } catch (e) {
      if (stamp !== generation.current || denied(e)) return;
      if (status(e) === 404) { setEditor(null); setItems([]); setVersions(null); pending.current = null; knownAck.current = null; setUncertain(false); setError('Workflow không còn khả dụng. Tải lại danh sách.'); }
      else if (status(e) === 412) {
        try { const latest = await automationGet(editor.id!); if (stamp === generation.current) { pending.current = null; setUncertain(false); setConflict(latest); setFormError('Revision đã thay đổi. So sánh trước khi gửi một command mới.'); } }
        catch (readError) { if (stamp === generation.current && !denied(readError)) { setUncertain(true); setFormError('Chưa tải được revision hiện tại. Retry cùng command.'); } }
      } else if (knownAck.current || status(e) === undefined || status(e) === 0 || (status(e) ?? 0) >= 500 || code(e) === 'RequestInProgress' || code(e) === 'ConcurrencyConflict') { setUncertain(true); setFormError('Chưa đối chiếu được kết quả. Retry giữ nguyên body, ETag và key.'); }
      else { pending.current = null; setUncertain(false); setFormError(message(e)); }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function reapply() {
    if (!conflict || !editor || flight.current) return;
    const compared = conflict, stamp = generation.current; flight.current = true; setBusy(true);
    try { const latest = await automationGet(compared.definition.id); if (stamp !== generation.current) return;
      if (latest.definition.etag !== compared.definition.etag) { setConflict(latest); setFormError('Revision lại thay đổi; hãy so sánh lại.'); return; }
      if (latest.definition.lifecycle !== "Active") { setFormError("Workflow hiện trong Trash. Khôi phục trước khi lưu bản nháp."); return; }
      setEditor({ ...editor, id: latest.definition.id, etag: latest.definition.etag, importing: false }); setConflict(null); pending.current = null; setUncertain(false); setFormError('Bản nháp giữ nguyên. Bấm lưu để tạo version mới trên revision đã so sánh.');
    } catch (e) { if (stamp === generation.current && !denied(e)) setFormError(message(e)); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function history(id: string, more = false) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true);
    try { const page = await automationHistory(id, more ? versions?.next ?? undefined : undefined); if (stamp === generation.current) setVersions({ id, items: more ? [...(versions?.items ?? []), ...page.items] : page.items, next: page.nextVersion }); }
    catch (e) { if (stamp === generation.current && !denied(e)) { setVersions(null); setError(message(e)); } }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function importFile(file?: File) {
    if (!file || editor || flight.current) return;
    const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try {
      if (file.size > 256 * 1024) throw new Error('File import vượt 256 KiB.');
      const value = JSON.parse(await file.text()) as { schemaVersion: number; title: string; definitionJson: string };
      if (!value || value.schemaVersion !== 1 || typeof value.title !== 'string' || typeof value.definitionJson !== 'string' || Object.keys(value).sort().join(',') !== 'definitionJson,schemaVersion,title') throw new Error('Portable envelope không hợp lệ.');
      const imported = JSON.parse(value.definitionJson) as AutomationShape;
      if (imported?.schemaVersion !== 1 || imported.trigger?.key !== 'manual' || imported.trigger.version !== 1 || !Array.isArray(imported.steps) || imported.steps.length < 1 || imported.steps.length > 20) throw new Error('Definition chưa được editor hỗ trợ.');
      const checked = await automationValidate({ title: value.title, definitionJson: value.definitionJson });
      if (stamp !== generation.current) return;
      if (!checked.shapeAccepted) throw new Error(checked.code);
      setShape(imported); setTitle(value.title); setEditor({ id: null, etag: null, importing: true, baseline: '' });
      setFormError('Import tạo Draft mới. Kiểm tra và rebind Project đích trước khi lưu; quyền và target sẽ được kiểm tra lại trên server.');
    } catch (e) { if (stamp === generation.current && !denied(e)) setError(message(e)); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function exportFile(id: string) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try {
      const value = await automationExport(id); if (stamp !== generation.current) return;
      const url = URL.createObjectURL(new Blob([JSON.stringify(value, null, 2)], { type: 'application/json' }));
      const link = document.createElement('a'); link.href = url; link.download = 'nexora-automation.json'; link.click(); setTimeout(() => URL.revokeObjectURL(url), 0);
    } catch (e) { if (stamp === generation.current && !denied(e)) setError(message(e)); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  const locked = busy || uncertain || conflict !== null;
  function stepInput(index: number, field: keyof AutomationStep['inputs'], value: string) {
    setShape(current => ({ ...current, steps: current.steps.map((step, i) => i === index ? { ...step, inputs: { ...step.inputs, [field]: value } } : step) })); setFormError('');
  }
  return <section className="content-section" aria-labelledby="automation-title">
    <div className="content-heading"><div><p className="eyebrow">FX34 / CONFIGURATION</p><h1 id="automation-title">Automations</h1><p className="lead">Lưu cấu hình và version bất biến. Execution, schedule và DryRun chưa khả dụng.</p></div><button type="button" disabled={loading || busy || !!lifecycleItem || !!editor || !!versions || !can('create')} onClick={() => begin()}>Workflow mới</button></div>
    {error && <p role="alert">{error}</p>}
    <label>Lifecycle<select value={lifecycle} disabled={busy || !!editor || !!lifecycleItem || !!versions} onChange={e => setLifecycle(e.target.value)}><option>Active</option><option>Trash</option></select></label>
    <label>Import definition<input type="file" accept=".json,application/json" disabled={busy || loading || !!editor || !!versions || !!lifecycleItem || !can('import') || !can('validate')} onChange={e => { const file = e.target.files?.[0]; e.target.value = ''; void importFile(file); }} /></label>
    {lifecycleItem && <AutomationLifecycleDialog item={lifecycleItem} onDenied={denied} onClose={() => { setLifecycleItem(null); void load(); }} />}

    <form onSubmit={e => { e.preventDefault(); if (!busy && !loading && !lifecycleItem) setFilter(query.trim()); }}><label>Tìm theo tên<input value={query} maxLength={200} disabled={busy || !!editor} onChange={e => setQuery(e.target.value)} /></label><button disabled={loading || busy || !!lifecycleItem || !!editor}>Tìm</button></form>
    <button type="button" disabled={loading || busy || !!lifecycleItem || !!editor} onClick={() => void load()}>Tải lại</button>
    {loading ? <p role="status">Đang tải workflows…</p> : !error && items.length === 0 ? <p>Chưa có workflow khớp bộ lọc.</p> : <div className="resource-cards">{items.map(item => <article className="resource-card" key={item.id}><h2>{item.title}</h2><p>Manual · {item.state} · Version {item.currentVersion} · {item.enabled ? 'Enabled' : 'Disabled'}</p><p>{new Date(item.updatedAt).toLocaleString()}</p><button type="button" disabled={busy || !!editor || !!versions || !!lifecycleItem || item.lifecycle !== "Active"} onClick={() => void open(item)}>Mở cấu hình</button><button type="button" disabled={busy || !!editor || !!lifecycleItem || !can('history')} onClick={() => void history(item.id)}>Lịch sử version</button><button type="button" disabled={busy || !!editor || !!versions || !!lifecycleItem || item.lifecycle !== 'Active' || !can('export')} onClick={() => void exportFile(item.id)}>Export</button><button type="button" disabled={busy || !!editor || !!versions || !!lifecycleItem || !can(item.lifecycle === 'Trash' ? 'restore' : 'trash')} onClick={() => setLifecycleItem(item)}>{item.lifecycle === 'Trash' ? 'Khôi phục' : 'Trash'}</button></article>)}</div>}
    {cursor && !error && <button type="button" disabled={loading || busy || !!lifecycleItem || !!editor} onClick={() => void load(true)}>Tải thêm 25 workflows</button>}
    {versions && <ResourceFormDialog open title="Lịch sử definition" busy={busy} dirty={false} onClose={() => setVersions(null)}>
      {versions.items.map(version => <details key={version.versionNumber}><summary>Version {version.versionNumber} · {version.title} · {new Date(version.createdAt).toLocaleString()}</summary><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{version.definitionJson}</pre></details>)}
      {versions.next && <button type="button" disabled={busy} onClick={() => void history(versions.id, true)}>Version cũ hơn</button>}
    </ResourceFormDialog>}
    {editor && <ResourceFormDialog open title={editor.id ? 'Definition editor' : editor.importing ? 'Import definition' : 'Workflow mới'} busy={busy} dirty={dirty} onClose={() => { if (uncertain) setFormError('Retry cùng command trước khi đóng.'); else { setEditor(null); pending.current = null; } }}>
      {formError && <p role="alert">{formError}</p>}
      {conflict && <section aria-label="Revision hiện tại"><h3>{conflict.definition.title} · Version {conflict.definition.currentVersion}</h3><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{conflict.version.definitionJson}</pre><button type="button" disabled={busy} onClick={() => void reapply()}>Áp dụng bản nháp trên revision này</button></section>}
      <form onSubmit={save}><fieldset disabled={locked || (editor.id ? !can('save') : !can(editor.importing ? 'import' : 'create'))}>
        <label>Tên workflow<input value={title} maxLength={200} required onChange={e => setTitle(e.target.value)} /></label>
        <p>Trigger: Manual v1</p>
        {shape.steps.map((step, index) => <fieldset key={step.id}><legend>Step {index + 1} · tasks.task.create v1</legend>
          <label>Project ID<input value={step.inputs.projectId} required onChange={e => stepInput(index, 'projectId', e.target.value)} /></label>
          <label>Task title<input value={step.inputs.title} maxLength={200} required onChange={e => stepInput(index, 'title', e.target.value)} /></label>
          <label>Mô tả<textarea value={step.inputs.description ?? ''} maxLength={4000} onChange={e => stepInput(index, 'description', e.target.value)} /></label>
          <label>Bắt đầu<input type="datetime-local" value={step.inputs.startAt.includes('T') && /(?:Z|[+-]\d{2}:\d{2})$/.test(step.inputs.startAt) ? localTime(step.inputs.startAt) : step.inputs.startAt} required onChange={e => stepInput(index, 'startAt', e.target.value)} /></label>
          <label>Kết thúc<input type="datetime-local" value={step.inputs.endAt.includes('T') && /(?:Z|[+-]\d{2}:\d{2})$/.test(step.inputs.endAt) ? localTime(step.inputs.endAt) : step.inputs.endAt} required onChange={e => stepInput(index, 'endAt', e.target.value)} /></label>
          <label>Priority<select value={step.inputs.priority} onChange={e => stepInput(index, 'priority', e.target.value)}>{['P0', 'P1', 'P2', 'P3'].map(value => <option key={value}>{value}</option>)}</select></label>
          <button type="button" disabled={shape.steps.length === 1} onClick={() => setShape(current => ({ ...current, steps: current.steps.filter(value => value.id !== step.id) }))}>Xóa step</button>
          <button type="button" disabled={index === 0} onClick={() => setShape(current => { const steps = [...current.steps]; [steps[index - 1], steps[index]] = [steps[index], steps[index - 1]]; return { ...current, steps }; })}>Lên</button>
        </fieldset>)}
        <button type="button" disabled={shape.steps.length >= 20} onClick={() => setShape(current => ({ ...current, steps: [...current.steps, emptyStep(Math.max(...current.steps.map(step => step.id)) + 1)] }))}>Thêm action step</button>
      </fieldset>
      <p>Thời gian nhập theo múi giờ trình duyệt; lưu timestamp có offset. Cấu hình không tạo Task.</p>
      <button type="button" disabled={locked || !can('validate')} onClick={() => void validate()}>Validate cấu trúc và target</button>
      <button className="primary-button" disabled={busy || !!conflict || (editor.id ? !can('save') : !can(editor.importing ? 'import' : 'create'))}>{busy ? 'Đang lưu…' : uncertain ? 'Retry cùng command' : 'Lưu version'}</button>
      </form>
    </ResourceFormDialog>}
  </section>;
}
