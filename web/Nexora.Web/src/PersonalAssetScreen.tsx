import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { assetCapabilities, assetGet, assetList, assetPreview, assetSave, assetSetState, assetTransition, type PersonalAsset, type AssetOperation, type AssetPreview, type AssetVersion, type AssetHistoryFilter, operationalStates, assetKinds, assetHistory } from './personalAssetApi';

type Draft = { title: string; kind: string; state: string; brand: string; model: string; category: string; notes: string; reason: string };
const emptyDraft: Draft = { title: '', kind: '', state: '', brand: '', model: '', category: '', notes: '', reason: '' };
const names: Record<AssetOperation, string> = { archive: 'Archive', unarchive: 'Unarchive', trash: 'Move to Trash', restore: 'Restore', purge: 'Delete permanently' };
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';

export function AssetScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const fieldPrefix = useId();
  const [caps, setCaps] = useState<Record<string, boolean>>({}); const [items, setItems] = useState<PersonalAsset[]>([]);
  const [filter, setFilter] = useState({ state: 'Active', query: '', kind: '', category: '' }); const [query, setQuery] = useState('');
  const [cursor, setCursor] = useState<string | null>(null); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null); const [formError, setFormError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ item?: PersonalAsset; mode: 'metadata' | 'state'; baseline: string } | null>(null); const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [historyFilter, setHistoryFilter] = useState<AssetHistoryFilter>({ version: "", action: "", from: "", to: "" });
  const [historyApplied, setHistoryApplied] = useState<AssetHistoryFilter>({ version: "", action: "", from: "", to: "" });
  const [history, setHistory] = useState<{ id: string; title: string; items: AssetVersion[]; cursor: string | null } | null>(null);
  const [pending, setPending] = useState<{ preview: AssetPreview; title: string } | null>(null);
  const [conflict, setConflict] = useState(false); const [currentVersion, setCurrentVersion] = useState<PersonalAsset | null>(null); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const sequence = useRef(0); const key = useRef(crypto.randomUUID()); const inFlight = useRef(false);
  const fail = useCallback((e: unknown) => {
    const status = (e as { status?: number }).status; setError(message(e));
    if (status === 401 || status === 403) { sequence.current++; setLoading(false); setBusy(false); setItems([]); setCaps({}); setHistory(null); setCursor(null); setEditor(null); setDraft(emptyDraft); setPending(null); setLeave(null); setConflict(false); setCurrentVersion(null); if (status === 401) onAuthLost(); }
  }, [onAuthLost]);
  const load = useCallback(async () => {
    const stamp = ++sequence.current; setHistory(null); setLoading(true); setError(null);
    try { const allowed = await assetCapabilities(); const page = allowed['assets.asset.read'] ? await assetList(filter.state, filter.query, filter.kind, filter.category) : null;
      if (stamp !== sequence.current) return; setCaps(allowed); setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null);
      if (!allowed['assets.asset.read']) { setEditor(null); setDraft(emptyDraft); setPending(null); setFormError(null); setLeave(null); setConflict(false); setCurrentVersion(null); }
    } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); }
  }, [filter, fail]);
  useEffect(() => { void load(); return () => { sequence.current++; }; }, [load]);
  const dirty = editor !== null && editor.baseline !== JSON.stringify(draft);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (busy && !editor) return true; if (!dirty && !busy) return false; setLeave({ proceed }); return true; }), [dirty, busy, editor]);
  useEffect(() => {
    if (!dirty && !busy) return;
    const prevent = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent);
  }, [dirty, busy]);
  const can = (action: string) => caps['assets.asset.' + action] === true;
  function begin(item?: PersonalAsset, mode: 'metadata' | 'state' = 'metadata') {
    const next = item ? { title: item.title, kind: item.kind, state: item.state, brand: item.brand ?? '', model: item.model ?? '', category: item.category ?? '', notes: item.notes ?? '', reason: '' } : emptyDraft;
    setDraft(next); setEditor({ item, mode, baseline: JSON.stringify(next) }); setFormError(null); setConflict(false); setCurrentVersion(null); key.current = crypto.randomUUID();
  }
  async function save(event: FormEvent) {
    event.preventDefault(); await persist();
  }
  async function persist() {
    if (!editor || inFlight.current || conflict) return false; inFlight.current = true; setBusy(true); setFormError(null);
    try { if (editor.mode === 'state' && editor.item) await assetSetState(editor.item, draft.state, draft.reason, key.current); else await assetSave({ ...draft, brand: draft.brand || null, model: draft.model || null, category: draft.category || null, notes: draft.notes || null }, key.current, editor.item); setEditor(null); setDraft(emptyDraft); setCurrentVersion(null); await load(); return true; }
    catch (e) {
      setFormError(message(e)); const status = (e as { status?: number }).status;
      if (status === 412 && editor.item) { setConflict(true); try { setCurrentVersion(await assetGet(editor.item.id)); } catch (readError) { fail(readError); } }
      if ([401, 403].includes(status ?? 0)) fail(e); return false;
    }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function resolveConflict(reapply: boolean) {
    if (!editor?.item || inFlight.current) return; inFlight.current = true; setBusy(true);
    try {
      const current = await assetGet(editor.item.id);
      if (reapply && current.etag !== currentVersion?.etag) { setCurrentVersion(current); setFormError('Revision lại thay đổi. So sánh bản hiện tại mới trước khi Reapply.'); return; }
      if (current.state === 'Archived' || current.state === 'Trash') { setFormError('Mục đã Archived hoặc Trash. Hủy bản nháp và khôi phục mục trước khi sửa.'); return; }
      const next = { title: current.title, kind: current.kind, state: current.state, brand: current.brand ?? '', model: current.model ?? '', category: current.category ?? '', notes: current.notes ?? '', reason: '' };
      setEditor({ item: current, mode: editor.mode, baseline: JSON.stringify(next) }); if (!reapply) setDraft(next);
      setConflict(false); setCurrentVersion(null); setFormError(reapply ? 'Đã lấy revision mới. Kiểm tra bản nháp rồi chọn Save để áp dụng.' : null); key.current = crypto.randomUUID();
    } catch (e) { if ((e as { status?: number }).status === 404) { setEditor(null); setDraft(emptyDraft); } fail(e); }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function preview(item: PersonalAsset, operation: AssetOperation) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true);
    try { const result = await assetPreview(item, operation); key.current = crypto.randomUUID(); setPending({ preview: result, title: item.title }); }
    catch (e) { fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function commit() {
    if (!pending || inFlight.current) return false; inFlight.current = true; setBusy(true);
    try { await assetTransition(pending.preview, key.current); await load(); return true as const; }
    catch (e) { if ((e as { status?: number }).status === 412) { setPending(null); await load(); } fail(e); return { error: message(e) }; } finally { inFlight.current = false; setBusy(false); }
  }
  const blocked = loading || busy;
  return <section className="content-section" aria-labelledby="asset-title">
    <div className="content-heading"><div><p className="eyebrow">FX37 / PERSONAL ASSETS</p><h1 id="asset-title">Assets</h1><p>Tài sản cá nhân, trạng thái và lịch sử riêng tư.</p></div><button className="secondary-button" disabled={blocked} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    <button className="primary-button" disabled={!can('create') || blocked} onClick={() => begin()}>New Asset</button>
    {can('read') && <>
      <form className="resource-form" onSubmit={e => { e.preventDefault(); setFilter(old => ({ ...old, query })); }}>
        <label>Title or model search<input value={query} maxLength={200} onChange={e => setQuery(e.target.value)} /></label><button disabled={blocked}>Search Asset</button>
        <div><label htmlFor={`${fieldPrefix}-state`}>State</label><select id={`${fieldPrefix}-state`} value={filter.state} disabled={blocked} onChange={e => setFilter(old => ({ ...old, state: e.target.value }))}>{[...operationalStates, 'Archived', 'Trash'].map(state => <option key={state}>{state}</option>)}</select></div>
        <label htmlFor={fieldPrefix+'-kind-filter'}>Kind filter</label><select id={fieldPrefix+'-kind-filter'} value={filter.kind} disabled={blocked} onChange={e => setFilter(old => ({ ...old, kind: e.target.value }))}><option value="">All kinds</option>{assetKinds.map(kind => <option key={kind}>{kind}</option>)}</select><label>Category filter<input maxLength={100} value={filter.category} onChange={e => setFilter(old => ({ ...old, category: e.target.value }))} /></label>
        <button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter({ state: 'Active', query: '', kind: '', category: '' }); }}>Clear filters</button>
      </form>
      {!loading && !error && !items.length && <p>{filter.query ? 'Không có mục khớp bộ lọc.' : 'Chưa có mục trong trạng thái này.'}</p>}
      <div className="resource-cards">{items.map(item => {
        const operations: AssetOperation[] = item.state === 'Trash' ? ['restore', 'purge'] : item.state === 'Archived' ? ['unarchive', 'trash'] : ['archive', 'trash'];
        return <article key={item.id} className="resource-card"><div><h2>{item.title}</h2><p>{item.state} · {item.kind}</p>{item.brand && <p>Brand: {item.brand}</p>}{item.model && <p>Model: {item.model}</p>}{item.category && <p>Category: {item.category}</p>}{item.notes && <p>{item.notes}</p>}</div><div className="resource-actions">
          {operationalStates.includes(item.state as typeof operationalStates[number]) && <><button disabled={!can('update') || blocked} onClick={() => begin(item)}>Edit Asset</button><button disabled={!can('transition') || blocked} onClick={() => begin(item, 'state')}>Change state</button></>}
          <button disabled={!can('history') || blocked} onClick={async () => { const stamp = sequence.current; setBusy(true); try { const rows = await assetHistory(item.id); setHistoryFilter({ version: "", action: "", from: "", to: "" }); setHistoryApplied({ version: "", action: "", from: "", to: "" }); if (stamp === sequence.current) setHistory({ id: item.id, title: item.title, items: rows.items, cursor: rows.nextCursor }); } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setBusy(false); } }}>Asset history</button>
          {operations.map(op => <button key={op} disabled={!can(op) || blocked} onClick={() => void preview(item, op)}>{names[op]}</button>)}
        </div></article>;
      })}</div>
      {cursor && <button disabled={blocked} onClick={async () => { const stamp = sequence.current; setLoading(true); try { const page = await assetList(filter.state, filter.query, filter.kind, filter.category, cursor); if (stamp === sequence.current) { setItems(old => [...old, ...page.items]); setCursor(page.nextCursor); } } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); } }}>Load more Asset items</button>}
    </>}
    {history && <section aria-label="Asset history" className="resource-card"><h2>History: {history.title}</h2><p>Private immutable snapshots. Historical inspection is read-only.</p><button disabled={blocked} onClick={() => setHistory(null)}>Close history</button><form className="resource-form" onSubmit={async e => { e.preventDefault(); if (inFlight.current) return; const stamp = sequence.current; inFlight.current = true; setBusy(true); try { const rows = await assetHistory(history.id, undefined, historyFilter); if (stamp === sequence.current) { setHistoryApplied({ ...historyFilter }); setHistory({ ...history, items: rows.items, cursor: rows.nextCursor }); } } catch (e) { if (stamp === sequence.current) fail(e); } finally { inFlight.current = false; if (stamp === sequence.current) setBusy(false); } }}>
        <label>History version<input inputMode="numeric" maxLength={19} value={historyFilter.version} onChange={e => setHistoryFilter(old => ({ ...old, version: e.target.value }))} /></label>
        <label htmlFor={fieldPrefix+"-history-action"}>History action</label><select id={fieldPrefix+"-history-action"} value={historyFilter.action} onChange={e => setHistoryFilter(old => ({ ...old, action: e.target.value }))}><option value="">All actions</option>{['create','update','transition','archive','unarchive','trash','restore'].map(action => <option key={action} value={'assets.asset.'+action}>{action}</option>)}</select>
        <label>History from (UTC instant)<input placeholder="2026-10-01T00:00:00Z" value={historyFilter.from} maxLength={64} onChange={e => setHistoryFilter(old => ({ ...old, from: e.target.value }))} /></label>
        <label>History to (UTC, exclusive)<input placeholder="2026-10-02T00:00:00Z" value={historyFilter.to} maxLength={64} onChange={e => setHistoryFilter(old => ({ ...old, to: e.target.value }))} /></label><button disabled={blocked}>Filter history</button></form>{!history.items.length && <p>No history versions match these filters.</p>}{history.items.map(version => <article key={version.id}><h3>Version {version.versionNumber}</h3><p>{version.actionKey} · {new Date(version.createdAt).toLocaleString()}</p>{version.reason && <p>Reason: {version.reason}</p>}<dl>{Object.entries(version.snapshot.fields).map(([name, value]) => <div key={name}><dt>{name}</dt><dd>{value ?? '—'}</dd></div>)}</dl></article>)}{history.cursor && <button disabled={blocked} onClick={async () => { const stamp = sequence.current; setBusy(true); try { const rows = await assetHistory(history.id, history.cursor!, historyApplied); if (stamp === sequence.current) setHistory(old => old?.id === history.id ? { ...old, items: [...old.items, ...rows.items], cursor: rows.nextCursor } : old); } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setBusy(false); } }}>Load more Asset history</button>}</section>}
    <ResourceFormDialog open={editor !== null && leave === null} title={editor?.mode === 'state' ? 'Change asset state' : editor?.item ? 'Edit Asset' : 'New Asset'} busy={busy} dirty={dirty} onClose={() => { setEditor(null); setDraft(emptyDraft); }}>
      <form onSubmit={save} className="resource-form">{formError && <p role="alert">{formError}</p>}
        {conflict && <div><p>Revision đã thay đổi. Reload current thay bản nháp; Reapply draft giữ bản nháp để bạn kiểm tra và Save lại.</p>{currentVersion && <div aria-label="Current Asset version"><h3>Current version</h3><p>{currentVersion.state} · {currentVersion.etag} · {new Date(currentVersion.updatedAt).toLocaleString()}</p><dl>{(['title', 'kind', 'brand', 'model', 'category', 'notes', 'state'] as const).map(field => <div key={field}><dt>{field}</dt><dd>{currentVersion[field] ?? '—'}</dd></div>)}</dl></div>}<button type="button" disabled={busy} onClick={() => void resolveConflict(false)}>Reload current</button><button type="button" disabled={busy || !currentVersion} onClick={() => void resolveConflict(true)}>Reapply draft</button></div>}
        {(editor?.mode === 'state' || !editor?.item) && <div><label htmlFor={fieldPrefix+'-asset-state'}>Asset state</label><select id={fieldPrefix+'-asset-state'} required value={draft.state} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, state: e.target.value })); }}><option value="">Choose a state</option>{operationalStates.map(state => <option key={state}>{state}</option>)}</select></div>}
        {editor?.mode === 'state' && <label>Private reason<textarea maxLength={2000} value={draft.reason} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, reason: e.target.value })); }} /></label>}
        {editor?.mode !== 'state' && <>
          <label htmlFor={fieldPrefix+'-kind'}>Asset kind</label><select id={fieldPrefix+'-kind'} required value={draft.kind} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, kind: e.target.value })); }}><option value="">Choose a kind</option>{assetKinds.map(kind => <option key={kind}>{kind}</option>)}</select>
          {(['title', 'brand', 'model', 'category', 'notes'] as const).map((field, index) => <div key={field}><label htmlFor={fieldPrefix+'-'+field}>{['Title', 'Brand', 'Model', 'Category', 'Notes'][index]}</label>{field === 'notes' ? <textarea id={fieldPrefix+'-'+field} maxLength={20000} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: e.target.value })); }} /> : <input id={fieldPrefix+'-'+field} required={field === 'title'} maxLength={field === 'brand' || field === 'category' ? 100 : 200} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: e.target.value })); }} />}</div>)}
        </>}
        <button className="primary-button" disabled={busy || conflict}>Save Asset</button>
      </form>
    </ResourceFormDialog>
    {leave && <ActionDialog title="Bỏ thay đổi chưa lưu?" description="Bản nháp Asset chưa được lưu. Hủy để tiếp tục sửa." confirmLabel="Bỏ bản nháp" confirmDisabled={busy} onClose={() => { if (!inFlight.current) setLeave(null); }} onConfirm={() => { if (inFlight.current) return false; const proceed = leave.proceed; setEditor(null); setDraft(emptyDraft); setCurrentVersion(null); setLeave(null); proceed(); return true; }}><button disabled={busy || conflict} onClick={async () => { const proceed = leave.proceed; if (await persist()) { setLeave(null); proceed(); } else setLeave(null); }}>Save and leave</button></ActionDialog>}
    {pending && <ActionDialog title={names[pending.preview.operation]} description={<span>{pending.title}. {pending.preview.operation === 'purge' ? 'Xóa vĩnh viễn; không thể hoàn tác.' : 'Giữ trạng thái trước đó để khôi phục.'}{pending.preview.referenceCount > 0 && ` ${pending.preview.referenceCount} references giữ mục này.`}</span>} confirmLabel={names[pending.preview.operation]} confirmDisabled={pending.preview.operation === 'purge' && pending.preview.referenceCount > 0} onClose={() => setPending(null)} onConfirm={commit} />}
  </section>;
}
