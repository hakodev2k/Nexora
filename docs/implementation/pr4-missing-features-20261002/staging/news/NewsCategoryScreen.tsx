import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { newsCategoryCapabilities, newsCategoryCreate, newsCategoryGet, newsCategoryList, newsCategoryUpdate, type NewsCategoryRecord } from './newsCategoryApi';

const message = (error: unknown) => error instanceof Error ? error.message : 'Không thể hoàn tất thao tác.';

export function NewsCategoryScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const prefix = useId(), epoch = useRef(0), mounted = useRef(true), flight = useRef(false), writeKey = useRef(crypto.randomUUID());
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [items, setItems] = useState<NewsCategoryRecord[]>([]);
  const [query, setQuery] = useState(''), [filter, setFilter] = useState(''), [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ item?: NewsCategoryRecord; baseline: string } | null>(null), [name, setName] = useState('');
  const [formError, setFormError] = useState<string | null>(null), [uncertain, setUncertain] = useState(false);
  const [conflict, setConflict] = useState<NewsCategoryRecord | null>(null), [conflictLocked, setConflictLocked] = useState(false);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const currentEditor = useRef(editor); currentEditor.current = editor;
  const clear = useCallback(() => { setItems([]); setCursor(null); setEditor(null); setName(''); setConflict(null); setConflictLocked(false); setUncertain(false); setFormError(null); setLeave(null); setQuery(''); }, []);
  const fail = useCallback((error: unknown) => {
    const status = (error as { status?: number }).status; setError(message(error));
    if (status === 401 || status === 403 || status === 404) { epoch.current++; clear(); if (status !== 404) setCaps({}); setLoading(false); if (status === 401) onAuthLost(); }
  }, [clear, onAuthLost]);
  const load = useCallback(async () => {
    if (!mounted.current) return;
    const stamp = ++epoch.current; setLoading(true); setError(null);
    try {
      const allowed = await newsCategoryCapabilities();
      if (!mounted.current || stamp !== epoch.current) return;
      setCaps(allowed); const editing = currentEditor.current;
      if (editing && (!allowed[editing.item ? 'news.category.update' : 'news.category.create'] || (editing.item && !allowed['news.category.read']))) clear();
      const page = allowed['news.category.read'] ? await newsCategoryList(filter) : null;
      if (!mounted.current || stamp !== epoch.current) return;
      setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null);
    } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { if (mounted.current && stamp === epoch.current) setLoading(false); }
  }, [filter, clear, fail]);
  useEffect(() => { mounted.current = true; void load(); return () => { mounted.current = false; epoch.current++; }; }, [load]);
  const dirty = editor !== null && editor.baseline !== name;
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !busy && !uncertain) return false; setLeave({ proceed }); return true; }), [dirty, busy, uncertain]);
  useEffect(() => { if (!dirty && !busy && !uncertain) return; const prevent = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ''; }; window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent); }, [dirty, busy, uncertain]);
  const can = (action: string) => caps['news.category.' + action] === true;
  function begin(item?: NewsCategoryRecord) {
    const next = item?.metadata.name ?? ''; setEditor({ item, baseline: next }); setName(next);
    setConflict(null); setConflictLocked(false); setFormError(null); setUncertain(false); writeKey.current = crypto.randomUUID();
  }
  async function persist() {
    if (!editor || flight.current || conflictLocked || !can(editor.item ? 'update' : 'create') || (editor.item && !can('read'))) return false;
    if (name.trim().length < 1 || name.trim().length > 100) { setFormError('Enter a category name from 1 to 100 characters.'); return false; }
    const metadata = { schemaVersion: 1 as const, name }; const stamp = epoch.current;
    flight.current = true; setBusy(true); setFormError(null);
    try {
      if (editor.item) await newsCategoryUpdate(editor.item, metadata, writeKey.current); else await newsCategoryCreate(metadata, writeKey.current);
      if (!mounted.current || stamp !== epoch.current) return false;
      setEditor(null); setName(''); setUncertain(false); setConflict(null); await load(); return true;
    } catch (e) {
      if (!mounted.current || stamp !== epoch.current) return false;
      setFormError(message(e)); const status = (e as { status?: number }).status;
      if (status === 0 || status === 503 || (status === 409 && (e as { code?: string }).code === 'RequestInProgress')) setUncertain(true);
      if (status === 412 && editor.item) {
        setUncertain(false); setConflictLocked(true);
        try { const current = await newsCategoryGet(editor.item.id); if (mounted.current && stamp === epoch.current) setConflict(current); }
        catch (readError) { if (mounted.current && stamp === epoch.current) fail(readError); }
      }
      if (status === 401 || status === 403 || status === 404) fail(e); return false;
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function recover(reapply: boolean) {
    if (!editor?.item || flight.current || !can('read') || !can('update')) return;
    const stamp = epoch.current; flight.current = true; setBusy(true);
    try {
      const current = await newsCategoryGet(editor.item.id); if (!mounted.current || stamp !== epoch.current) return;
      if (reapply && current.etag !== conflict?.etag) { setConflict(current); setFormError('Revision changed again. Compare the new current name before reapplying.'); return; }
      setEditor({ item: current, baseline: current.metadata.name }); if (!reapply) setName(current.metadata.name);
      setConflict(null); setConflictLocked(false); setUncertain(false); writeKey.current = crypto.randomUUID();
      setFormError(reapply ? 'Draft retained. Review and choose Save to apply it to the current version.' : null);
    } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function more() {
    if (!cursor || flight.current || editor || !can('read')) return;
    flight.current = true; const stamp = epoch.current; setLoading(true); setError(null);
    try { const next = await newsCategoryList(filter, cursor); if (mounted.current && stamp === epoch.current) { setItems(old => [...old, ...next.items.filter(item => !old.some(existing => existing.id === item.id))]); setCursor(next.nextCursor); } }
    catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { flight.current = false; if (mounted.current && stamp === epoch.current) setLoading(false); }
  }
  const blocked = loading || busy || editor !== null;
  return <section className="content-section" aria-labelledby={`${prefix}-title`}>
    <div className="content-heading"><div><p className="eyebrow">FX29 / NEWS</p><h1 id={`${prefix}-title`}>News categories</h1><p>Private category containers. Feed fetching, articles and topic watches are unavailable in this local slice.</p></div><button className="secondary-button" disabled={blocked} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}{!loading && !error && !Object.values(caps).some(Boolean) && <p role="status">News categories are unavailable for the current module or permissions.</p>}
    {can('create') && <button className="primary-button" disabled={blocked} onClick={() => begin()}>New category</button>}
    {!loading && !can('read') && can('create') && <p>Category creation is available. Reading saved categories requires separate permission.</p>}
    {can('read') && <><form className="resource-form" onSubmit={e => { e.preventDefault(); if (!blocked) setFilter(query); }}>
      <label htmlFor={`${prefix}-query`}>Category name search</label><input id={`${prefix}-query`} maxLength={100} value={query} disabled={blocked} onChange={e => setQuery(e.target.value)} /><button disabled={blocked}>Search categories</button>
      <button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter(''); }}>Clear filters</button>
    </form>{!loading && !error && items.length === 0 && <p>{filter ? 'No categories match this name query.' : 'No saved categories.'}</p>}
    <div className="resource-cards">{items.map(item => <article key={item.id} className="resource-card"><div><h2>{item.metadata.name}</h2><p>Updated {new Date(item.updatedAt).toLocaleString()}</p></div><div className="resource-actions">{can('update') && <button disabled={blocked} onClick={() => begin(item)}>Edit category</button>}</div></article>)}</div>
    {cursor && <button disabled={blocked} onClick={() => void more()}>Load more categories</button>}</>}
    <ResourceFormDialog open={editor !== null && leave === null} title={editor?.item ? 'Edit category' : 'New category'} busy={busy} dirty={dirty && !uncertain} onClose={() => { if (uncertain) { setLeave({ proceed: () => { setEditor(null); setName(''); setConflict(null); setConflictLocked(false); setUncertain(false); void load(); } }); return; } setEditor(null); setName(''); setConflict(null); setConflictLocked(false); setUncertain(false); void load(); }}>
      <form className="resource-form" onSubmit={(e: FormEvent) => { e.preventDefault(); void persist(); }}>{formError && <p role="alert">{formError}</p>}
        {uncertain && <p role="status">The save outcome is unknown. Retry the same request to recover its acknowledgement; discarding may leave a saved server category.</p>}
        {conflictLocked && <div><p>Revision changed. Compare the current saved name before replacing or reapplying your draft.</p>{conflict && <div aria-label="Current category version"><h3>Current version</h3><p>{conflict.metadata.name}</p></div>}<button type="button" disabled={busy} onClick={() => void recover(false)}>Reload current</button><button type="button" disabled={busy || conflict === null} onClick={() => void recover(true)}>Reapply draft</button></div>}
        <label htmlFor={`${prefix}-name`}>Category name</label><input id={`${prefix}-name`} required maxLength={100} value={name} disabled={busy || uncertain} onChange={e => { if (flight.current || uncertain) return; writeKey.current = crypto.randomUUID(); setName(e.target.value); }} />
        <button className="primary-button" disabled={busy || conflictLocked || !can(editor?.item ? 'update' : 'create') || (!!editor?.item && !can('read'))}>{uncertain ? 'Retry same save' : 'Save category'}</button>
      </form>
    </ResourceFormDialog>
    {leave && <ActionDialog title="Bỏ thay đổi chưa lưu?" description={uncertain ? 'The save outcome is unknown. Discarding leaves any committed server category intact.' : 'Bản nháp category chưa được lưu.'} confirmLabel="Bỏ bản nháp" confirmDisabled={busy} onClose={() => { if (!flight.current) setLeave(null); }} onConfirm={() => { if (flight.current) return false; const proceed = leave.proceed; setEditor(null); setName(''); setConflict(null); setConflictLocked(false); setUncertain(false); setLeave(null); proceed(); return true; }}><button disabled={busy || conflictLocked || editor === null} onClick={async () => { const proceed = leave.proceed; if (await persist()) { setLeave(null); proceed(); } else setLeave(null); }}>Save and leave</button></ActionDialog>}
  </section>;
}
