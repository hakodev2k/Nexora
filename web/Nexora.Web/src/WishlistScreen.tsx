import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { wishlistCapabilities, wishlistGet, wishlistList, wishlistPreview, wishlistSave, wishlistTransition, type WishlistItem, type WishlistOperation, type WishlistPreview } from './wishlistApi';

type Draft = { title: string; quantity: string; targetAmount: string; currency: string; url: string; notes: string };
const emptyDraft: Draft = { title: '', quantity: '1', targetAmount: '', currency: '', url: '', notes: '' };
const names: Record<WishlistOperation, string> = { 'mark-purchased': 'Mark purchased', archive: 'Archive', unarchive: 'Unarchive', trash: 'Move to Trash', restore: 'Restore', purge: 'Delete permanently' };
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';

export function WishlistScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const fieldPrefix = useId();
  const [caps, setCaps] = useState<Record<string, boolean>>({}); const [items, setItems] = useState<WishlistItem[]>([]);
  const [filter, setFilter] = useState({ status: 'Wanted', query: '' }); const [query, setQuery] = useState('');
  const [cursor, setCursor] = useState<string | null>(null); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null); const [formError, setFormError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ item?: WishlistItem; baseline: string } | null>(null); const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [pending, setPending] = useState<{ preview: WishlistPreview; title: string } | null>(null);
  const [conflict, setConflict] = useState(false); const [currentVersion, setCurrentVersion] = useState<WishlistItem | null>(null); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const sequence = useRef(0); const key = useRef(crypto.randomUUID()); const inFlight = useRef(false);
  const fail = useCallback((e: unknown) => {
    const status = (e as { status?: number }).status; setError(message(e));
    if (status === 401 || status === 403) { sequence.current++; setLoading(false); setItems([]); setCaps({}); setCursor(null); setEditor(null); setDraft(emptyDraft); setPending(null); setLeave(null); setConflict(false); setCurrentVersion(null); if (status === 401) onAuthLost(); }
  }, [onAuthLost]);
  const load = useCallback(async () => {
    const stamp = ++sequence.current; setLoading(true); setError(null);
    try { const allowed = await wishlistCapabilities(); const page = allowed['shopping.wishlist.read'] ? await wishlistList(filter.status, filter.query) : null;
      if (stamp !== sequence.current) return; setCaps(allowed); setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null);
      if (!allowed['shopping.wishlist.read']) { setEditor(null); setDraft(emptyDraft); setPending(null); setFormError(null); setLeave(null); setConflict(false); setCurrentVersion(null); }
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
  const can = (action: string) => caps['shopping.wishlist.' + action] === true;
  function begin(item?: WishlistItem) {
    const next = item ? { title: item.title, quantity: item.quantity, targetAmount: item.targetAmount ?? '', currency: item.currency ?? '', url: item.url ?? '', notes: item.notes ?? '' } : emptyDraft;
    setDraft(next); setEditor({ item, baseline: JSON.stringify(next) }); setFormError(null); setConflict(false); setCurrentVersion(null); key.current = crypto.randomUUID();
  }
  async function save(event: FormEvent) {
    event.preventDefault(); await persist();
  }
  async function persist() {
    if (!editor || inFlight.current || conflict) return false; inFlight.current = true; setBusy(true); setFormError(null);
    try { await wishlistSave({ ...draft, targetAmount: draft.targetAmount || null, currency: draft.currency || null, url: draft.url || null, notes: draft.notes || null }, key.current, editor.item); setEditor(null); setDraft(emptyDraft); setCurrentVersion(null); await load(); return true; }
    catch (e) {
      setFormError(message(e)); const status = (e as { status?: number }).status;
      if (status === 412 && editor.item) { setConflict(true); try { setCurrentVersion(await wishlistGet(editor.item.id)); } catch (readError) { fail(readError); } }
      if ([401, 403].includes(status ?? 0)) fail(e); return false;
    }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function resolveConflict(reapply: boolean) {
    if (!editor?.item || inFlight.current) return; inFlight.current = true; setBusy(true);
    try {
      const current = await wishlistGet(editor.item.id);
      if (reapply && current.etag !== currentVersion?.etag) { setCurrentVersion(current); setFormError('Revision lại thay đổi. So sánh bản hiện tại mới trước khi Reapply.'); return; }
      if (current.status === 'Archived' || current.status === 'Trash') { setFormError('Mục đã Archived hoặc Trash. Hủy bản nháp và khôi phục mục trước khi sửa.'); return; }
      const next = { title: current.title, quantity: current.quantity, targetAmount: current.targetAmount ?? '', currency: current.currency ?? '', url: current.url ?? '', notes: current.notes ?? '' };
      setEditor({ item: current, baseline: JSON.stringify(next) }); if (!reapply) setDraft(next);
      setConflict(false); setCurrentVersion(null); setFormError(reapply ? 'Đã lấy revision mới. Kiểm tra bản nháp rồi chọn Save để áp dụng.' : null); key.current = crypto.randomUUID();
    } catch (e) { if ((e as { status?: number }).status === 404) { setEditor(null); setDraft(emptyDraft); } fail(e); }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function preview(item: WishlistItem, operation: WishlistOperation) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true);
    try { const result = await wishlistPreview(item, operation); key.current = crypto.randomUUID(); setPending({ preview: result, title: item.title }); }
    catch (e) { fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function commit() {
    if (!pending || inFlight.current) return false; inFlight.current = true; setBusy(true);
    try { await wishlistTransition(pending.preview, key.current); await load(); return true as const; }
    catch (e) { if ((e as { status?: number }).status === 412) { setPending(null); await load(); } fail(e); return { error: message(e) }; } finally { inFlight.current = false; setBusy(false); }
  }
  const blocked = loading || busy;
  return <section className="content-section" aria-labelledby="wishlist-title">
    <div className="content-heading"><div><p className="eyebrow">FX31 / SHOPPING</p><h1 id="wishlist-title">Wishlist</h1><p>Danh sách mua sắm cá nhân. Orders, comparison và checkout chưa khả dụng trong phần này.</p></div><button className="secondary-button" disabled={blocked} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    <button className="primary-button" disabled={!can('create') || blocked} onClick={() => begin()}>New Wishlist item</button>
    {can('read') && <>
      <form className="resource-form" onSubmit={e => { e.preventDefault(); setFilter(old => ({ ...old, query })); }}>
        <label>Title search<input value={query} maxLength={200} onChange={e => setQuery(e.target.value)} /></label><button disabled={blocked}>Search Wishlist</button>
        <div><label htmlFor={`${fieldPrefix}-status`}>Status</label><select id={`${fieldPrefix}-status`} value={filter.status} disabled={blocked} onChange={e => setFilter(old => ({ ...old, status: e.target.value }))}>{['Wanted', 'Purchased', 'Archived', 'Trash'].map(status => <option key={status}>{status}</option>)}</select></div>
        <button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter({ status: 'Wanted', query: '' }); }}>Clear filters</button>
      </form>
      {!loading && !error && !items.length && <p>{filter.query ? 'Không có mục khớp bộ lọc.' : 'Chưa có mục trong trạng thái này.'}</p>}
      <div className="resource-cards">{items.map(item => {
        const operations: WishlistOperation[] = item.status === 'Trash' ? ['restore', 'purge'] : item.status === 'Archived' ? ['unarchive', 'trash'] : item.status === 'Wanted' ? ['mark-purchased', 'archive', 'trash'] : ['archive', 'trash'];
        return <article key={item.id} className="resource-card"><div><h2>{item.title}</h2><p>{item.status} · Quantity: {item.quantity}</p>{item.targetAmount !== null && <p>Desired amount: {item.targetAmount} {item.currency}</p>}{item.url && <p className="muted">{item.url}</p>}{item.notes && <p>{item.notes}</p>}</div><div className="resource-actions">
          {['Wanted', 'Purchased'].includes(item.status) && <button disabled={!can('update') || blocked} onClick={() => begin(item)}>Edit Wishlist item</button>}
          {operations.map(op => <button key={op} disabled={!can(op.replace('-', '_')) || blocked} onClick={() => void preview(item, op)}>{names[op]}</button>)}
        </div></article>;
      })}</div>
      {cursor && <button disabled={blocked} onClick={async () => { const stamp = sequence.current; setLoading(true); try { const page = await wishlistList(filter.status, filter.query, cursor); if (stamp === sequence.current) { setItems(old => [...old, ...page.items]); setCursor(page.nextCursor); } } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); } }}>Load more Wishlist items</button>}
    </>}
    <ResourceFormDialog open={editor !== null && leave === null} title={editor?.item ? 'Edit Wishlist item' : 'New Wishlist item'} busy={busy} dirty={dirty} onClose={() => { setEditor(null); setDraft(emptyDraft); }}>
      <form onSubmit={save} className="resource-form">{formError && <p role="alert">{formError}</p>}
        {conflict && <div><p>Revision đã thay đổi. Reload current thay bản nháp; Reapply draft giữ bản nháp để bạn kiểm tra và Save lại.</p>{currentVersion && <div aria-label="Current Wishlist version"><h3>Current version</h3><p>{currentVersion.status} · {currentVersion.etag} · {new Date(currentVersion.updatedAt).toLocaleString()}</p><dl>{(['title', 'quantity', 'targetAmount', 'currency', 'url', 'notes'] as const).map(field => <div key={field}><dt>{field}</dt><dd>{currentVersion[field] ?? '—'}</dd></div>)}</dl></div>}<button type="button" disabled={busy} onClick={() => void resolveConflict(false)}>Reload current</button><button type="button" disabled={busy || !currentVersion} onClick={() => void resolveConflict(true)}>Reapply draft</button></div>}
        {(['title', 'quantity', 'targetAmount', 'currency', 'url', 'notes'] as const).map((field, index) => <div key={field}><label htmlFor={`${fieldPrefix}-${field}`}>{['Title', 'Quantity', 'Desired amount', 'Currency', 'URL', 'Notes'][index]}</label>{field === 'notes' ? <textarea id={`${fieldPrefix}-${field}`} maxLength={20000} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: e.target.value })); }} /> : <input id={`${fieldPrefix}-${field}`} required={field === 'title' || field === 'quantity'} maxLength={field === 'title' ? 200 : field === 'url' ? 2048 : field === 'currency' ? 3 : 30} inputMode={field === 'quantity' || field === 'targetAmount' ? 'decimal' : undefined} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: e.target.value })); }} />}</div>)}
        <button className="primary-button" disabled={busy || conflict}>Save Wishlist item</button>
      </form>
    </ResourceFormDialog>
    {leave && <ActionDialog title="Bỏ thay đổi chưa lưu?" description="Bản nháp Wishlist chưa được lưu. Hủy để tiếp tục sửa." confirmLabel="Bỏ bản nháp" confirmDisabled={busy} onClose={() => { if (!inFlight.current) setLeave(null); }} onConfirm={() => { if (inFlight.current) return false; const proceed = leave.proceed; setEditor(null); setDraft(emptyDraft); setCurrentVersion(null); setLeave(null); proceed(); return true; }}><button disabled={busy || conflict} onClick={async () => { const proceed = leave.proceed; if (await persist()) { setLeave(null); proceed(); } else setLeave(null); }}>Save and leave</button></ActionDialog>}
    {pending && <ActionDialog title={names[pending.preview.operation]} description={<span>{pending.title}. {pending.preview.operation === 'purge' ? 'Xóa vĩnh viễn; không thể hoàn tác.' : pending.preview.operation === 'mark-purchased' ? 'Chỉ ghi trạng thái cá nhân; không tạo Order hay giao dịch Finance.' : 'Giữ trạng thái trước đó để khôi phục.'}{pending.preview.referenceCount > 0 && ` ${pending.preview.referenceCount} references giữ mục này.`}</span>} confirmLabel={names[pending.preview.operation]} confirmDisabled={pending.preview.operation === 'purge' && pending.preview.referenceCount > 0} onClose={() => setPending(null)} onConfirm={commit} />}
  </section>;
}
