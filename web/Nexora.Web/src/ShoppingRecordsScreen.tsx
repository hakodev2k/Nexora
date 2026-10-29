import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ResourceFormDialog } from './ResourceFormDialog';
import { requestDirtyLeave } from './navigationGuard';
import { sendShoppingIntent, shoppingRecordCapabilities, shoppingRecordGet, shoppingRecordList,
  type SellerRecord, type ShoppingIntent, type ShoppingKind, type ShoppingOrder, type ShoppingOrderSummary,
  type ShoppingPage, type ShoppingRecord, type ShoppingRow, type WarrantyRecord } from './shoppingRecordsApi';
import './shoppingRecords.css';

type LineDraft = { id: string | null; title: string; quantity: string; unitAmount: string; variant: string };
type Draft = { title: string; url: string; sellerId: string; orderNumber: string; purchasedOn: string; currency: string;
  shippingAmount: string; taxAmount: string; discountAmount: string; notes: string; confirmDuplicateReference: boolean;
  lines: LineDraft[]; orderLineId: string; coverageKind: string; startsOn: string; endsOn: string; provider: string; terms: string; claimContact: string };
const blank = (): Draft => ({ title: '', url: '', sellerId: '', orderNumber: '', purchasedOn: '', currency: '', shippingAmount: '0', taxAmount: '0', discountAmount: '0', notes: '', confirmDuplicateReference: false,
  lines: [{ id: null, title: '', quantity: '1', unitAmount: '', variant: '' }], orderLineId: '', coverageKind: 'Unknown', startsOn: '', endsOn: '', provider: '', terms: '', claimContact: '' });
const definition = { sellers: { action: 'seller', title: 'Người bán', singular: 'người bán', hint: 'Lưu tên và website để tìm lại nơi bạn mua sắm.' },
  orders: { action: 'order', title: 'Đơn mua sắm', singular: 'đơn mua sắm', hint: 'Ghi lại sản phẩm, số lượng và chi phí của bạn trong một nơi.' },
  warranties: { action: 'warranty', title: 'Bảo hành', singular: 'bảo hành', hint: 'Giữ thông tin bảo hành cạnh sản phẩm để dễ tìm khi cần.' } };
const titleOf = (r: ShoppingRow) => 'title' in r ? r.title : r.orderNumber || 'Đơn ngày ' + r.purchasedOn;
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác. Vui lòng thử lại.';
function fromRecord(record: ShoppingRecord): Draft {
  const d = blank();
  if ('coverageKind' in record) return { ...d, title: record.title, orderLineId: record.orderLineId, coverageKind: record.coverageKind,
    startsOn: record.startsOn ?? '', endsOn: record.endsOn ?? '', provider: record.provider ?? '', terms: record.terms ?? '', claimContact: record.claimContact ?? '' };
  if ('lines' in record) return { ...d, sellerId: record.sellerId ?? '', orderNumber: record.orderNumber ?? '', purchasedOn: record.purchasedOn,
    currency: record.currency, shippingAmount: record.shippingAmount, taxAmount: record.taxAmount, discountAmount: record.discountAmount, notes: record.notes ?? '',
    lines: record.lines.map(l => ({ ...l, variant: l.variant ?? '' })) };
  return { ...d, title: record.title, url: record.url ?? '' };
}
function bodyFor(kind: ShoppingKind, d: Draft) {
  if (kind === 'sellers') return { title: d.title, url: d.url || null };
  if (kind === 'orders') return { sellerId: d.sellerId || null, orderNumber: d.orderNumber || null, purchasedOn: d.purchasedOn, currency: d.currency,
    shippingAmount: d.shippingAmount, taxAmount: d.taxAmount, discountAmount: d.discountAmount, notes: d.notes || null, confirmDuplicateReference: d.confirmDuplicateReference,
    lines: d.lines.map(l => ({ ...l, variant: l.variant || null })) };
  return { orderLineId: d.orderLineId, title: d.title, coverageKind: d.coverageKind, startsOn: d.coverageKind === 'Fixed' ? d.startsOn : null,
    endsOn: d.coverageKind === 'Fixed' ? d.endsOn : null, provider: d.provider || null, terms: d.terms || null, claimContact: d.claimContact || null };
}

export function ShoppingRecordsScreen({ kind = 'orders', initialRecordId, onAuthLost, onSelectRecord, onSectionChange }: {
  kind?: ShoppingKind; initialRecordId?: string; onAuthLost: () => void;
  onSelectRecord?: (id?: string) => void; onSectionChange?: (kind: ShoppingKind) => void;
}) {
  const prefix = useId(), sequence = useRef(0), mounted = useRef(true), flight = useRef(false), original = useRef<ShoppingIntent | null>(null);
  const authLost = useRef(onAuthLost); authLost.current = onAuthLost;
  const selectRecord = useRef(onSelectRecord); selectRecord.current = onSelectRecord;
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [page, setPage] = useState<ShoppingPage<ShoppingRow> | null>(null);
  const [filter, setFilter] = useState({ query: '', page: 1 }), [query, setQuery] = useState(''), [selected, setSelected] = useState(initialRecordId);
  const [detail, setDetail] = useState<ShoppingRecord | null>(null), [refresh, setRefresh] = useState(0), [pendingNavigation, setPendingNavigation] = useState<string | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ record?: ShoppingRecord; baseline: string } | null>(null), [draft, setDraft] = useState<Draft>(blank);
  const [formError, setFormError] = useState<string | null>(null), [uncertain, setUncertain] = useState(false), [abandon, setAbandon] = useState(false);
  const [conflict, setConflict] = useState<ShoppingRecord | null>(null), [conflictLocked, setConflictLocked] = useState(false), [duplicate, setDuplicate] = useState(false);
  const [sourceQuery, setSourceQuery] = useState(''), [sources, setSources] = useState<Array<SellerRecord | ShoppingOrderSummary>>([]), [sourcePage, setSourcePage] = useState(0), [sourceMore, setSourceMore] = useState(false);
  const [sourceAppliedQuery, setSourceAppliedQuery] = useState('');
  const [sourceError, setSourceError] = useState<string | null>(null), [sourceBusy, setSourceBusy] = useState(false), [sourceOrder, setSourceOrder] = useState<ShoppingOrder | null>(null);
  const sourceSequence = useRef(0);
  const spec = definition[kind], allowed = (action: string) => caps[`shopping.${spec.action}.${action}`] === true;
  const clear = useCallback(() => { setCaps({}); setPage(null); setDetail(null); setEditor(null); setDraft(blank()); setSources([]); setSourceOrder(null); setSourceBusy(false); setConflict(null); setConflictLocked(false); setUncertain(false); setAbandon(false); original.current = null; setFormError(null); }, []);
  const fail = useCallback((e: unknown) => {
    setError(message(e)); const status = (e as { status?: number }).status;
    if (status === 401 || status === 403) { sequence.current++; sourceSequence.current++; clear(); setLoading(false); if (status === 401) authLost.current(); }
  }, [clear]);
  useEffect(() => { setSelected(initialRecordId); }, [initialRecordId]);
  useEffect(() => {
    mounted.current = true; const stamp = ++sequence.current; setLoading(true); setError(null); setPage(null); setDetail(null);
    (async () => {
      try {
        const nextCaps = await shoppingRecordCapabilities(); if (!mounted.current || stamp !== sequence.current) return;
        setCaps(nextCaps);
        if (!nextCaps[`shopping.${definition[kind].action}.read`]) return;
        const next = await shoppingRecordList<ShoppingRow>(kind, filter.page, filter.query);
        const current = selected ? await shoppingRecordGet<ShoppingRecord>(kind, selected) : null;
        if (!mounted.current || stamp !== sequence.current) return;
        setPage(next); setDetail(current);
      } catch (e) { if (mounted.current && stamp === sequence.current) fail(e); }
      finally { if (mounted.current && stamp === sequence.current) setLoading(false); }
    })();
    return () => { mounted.current = false; sequence.current++; sourceSequence.current++; };
  }, [kind, selected, filter, refresh, fail]);
  useEffect(() => {
    if (!busy && !editor && pendingNavigation) { const id = pendingNavigation; setPendingNavigation(null); selectRecord.current?.(id); }
  }, [busy, editor, pendingNavigation]);
  const dirty = editor !== null && editor.baseline !== JSON.stringify(draft);
  const editLocked = busy || uncertain || conflictLocked;
  function change<K extends keyof Draft>(field: K, value: Draft[K]) { if (!editLocked && !flight.current) { original.current = null; setDraft(d => ({ ...d, [field]: value })); } }
  function begin(record?: ShoppingRecord) {
    sourceSequence.current++; setSourceBusy(false);
    const next = record ? fromRecord(record) : blank(); setDraft(next); setEditor({ record, baseline: JSON.stringify(next) });
    setFormError(null); setUncertain(false); setAbandon(false); setConflict(null); setConflictLocked(false); setDuplicate(false);
    setSources([]); setSourcePage(0); setSourceQuery(''); setSourceAppliedQuery(''); setSourceOrder(null); setSourceError(null); original.current = null;
  }
  async function open(id: string) { if (busy || editor) return; setSelected(id); onSelectRecord?.(id); }
  async function sourceSearch(more = false) {
    if (sourceBusy || editLocked || (more && sourceQuery !== sourceAppliedQuery)) return; const type = kind === 'orders' ? 'sellers' : 'orders';
    if (!caps[`shopping.${type === 'sellers' ? 'seller' : 'order'}.read`]) return;
    const stamp = ++sourceSequence.current, submittedQuery = sourceQuery; setSourceBusy(true); setSourceError(null);
    try {
      const result = await shoppingRecordList<SellerRecord | ShoppingOrderSummary>(type, more ? sourcePage + 1 : 1, submittedQuery);
      if (mounted.current && stamp === sourceSequence.current) { setSources(old => more ? [...old, ...result.items.filter(r => !old.some(existing => existing.id === r.id))] : result.items); setSourcePage(result.page); setSourceMore(result.hasMore); setSourceAppliedQuery(submittedQuery); }
    } catch (e) { if (mounted.current && stamp === sourceSequence.current) { setSourceError(message(e)); if ([401, 403].includes((e as { status?: number }).status ?? 0)) fail(e); } }
    finally { if (mounted.current && stamp === sourceSequence.current) setSourceBusy(false); }
  }
  async function chooseOrder(id: string) {
    if (editLocked || flight.current) return;
    if (!id) { sourceSequence.current++; setSourceOrder(null); change('orderLineId', ''); return; }
    setSourceOrder(null); change('orderLineId', '');
    const stamp = ++sourceSequence.current; setSourceBusy(true); setSourceError(null);
    try { const order = await shoppingRecordGet<ShoppingOrder>('orders', id); if (mounted.current && stamp === sourceSequence.current) {
      setSourceOrder(order);
      setDraft(current => order.lines.some(line => line.id === current.orderLineId) ? current : { ...current, orderLineId: '' });
    } }
    catch (e) { if (mounted.current && stamp === sourceSequence.current) { setSourceError(message(e)); if ([401, 403].includes((e as { status?: number }).status ?? 0)) fail(e); } }
    finally { if (mounted.current && stamp === sourceSequence.current) setSourceBusy(false); }
  }
  async function persist(e?: FormEvent) {
    e?.preventDefault(); if (!editor || flight.current || sourceBusy || conflictLocked || !allowed(editor.record ? 'update' : 'create')) return;
    const stamp = sequence.current;
    const intent = original.current ?? { kind, id: editor.record?.id, etag: editor.record?.etag, key: crypto.randomUUID(), body: JSON.stringify(bodyFor(kind, draft)) };
    original.current = intent; flight.current = true; setBusy(true); setFormError(null);
    try {
      const ack = await sendShoppingIntent(intent); if (!mounted.current || stamp !== sequence.current) return;
      setEditor(null); setUncertain(false); setAbandon(false); original.current = null; setNotice('Đã lưu.');
      if (allowed('read')) { setSelected(ack.id); setPendingNavigation(ack.id); } setRefresh(n => n + 1);
    } catch (error) {
      if (!mounted.current || stamp !== sequence.current) return;
      const status = (error as { status?: number }).status, code = (error as { code?: string }).code; setFormError(message(error));
      if (status === 0 || status === 408 || (status !== undefined && status >= 500) || (status === 409 && code === 'RequestInProgress')) setUncertain(true);
      else { original.current = null; setUncertain(false); }
      if (code === 'DuplicateOrderReference') setDuplicate(true);
      if (status === 412 && editor.record) {
        setConflictLocked(true);
        try { const current = await shoppingRecordGet<ShoppingRecord>(kind, editor.record.id); if (mounted.current && stamp === sequence.current) setConflict(current); }
        catch (readError) { if (mounted.current && stamp === sequence.current) fail(readError); }
      }
      if (status === 401 || status === 403) fail(error);
      if (status === 404) { setEditor(null); setDetail(null); setSelected(undefined); setSources([]); setSourceOrder(null); }
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function resolveConflict(reapply: boolean) {
    if (!editor?.record || busy) return; const stamp = sequence.current; flight.current = true; setBusy(true);
    try {
      const current = await shoppingRecordGet<ShoppingRecord>(kind, editor.record.id); if (!mounted.current || stamp !== sequence.current) return;
      if (reapply && current.etag !== conflict?.etag) { setConflict(current); setFormError('Bản ghi lại thay đổi. So sánh phiên bản mới trước khi áp dụng.'); return; }
      if ('status' in current && current.status !== 'Draft') { setFormError('Đơn không còn là bản nháp. Hãy đóng biểu mẫu và mở lại thông tin hiện tại.'); return; }
      const next = fromRecord(current); setEditor({ record: current, baseline: JSON.stringify(next) }); if (!reapply) setDraft(next);
      setConflict(null); setConflictLocked(false); original.current = null; setFormError(reapply ? 'Bản nháp được giữ lại. Kiểm tra rồi chọn Lưu.' : null);
    } catch (e) { if (mounted.current && stamp === sequence.current) fail(e); }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function discardUnknown() { if (busy) return; setEditor(null); setUncertain(false); setAbandon(false); original.current = null; setRefresh(n => n + 1); }
  const blocked = loading || busy || editor !== null;
  const field = (label: string, name: keyof Draft, max = 200, required = false, type = 'text') => <label>{label}<input type={type} maxLength={max} required={required} value={String(draft[name])} disabled={editLocked} onChange={e => change(name, e.target.value as never)} /></label>;
  return <section className="content-section shopping-records" aria-labelledby={`${prefix}-title`}>
    <div className="content-heading"><div><p className="eyebrow">MUA SẮM CÁ NHÂN</p><h1 id={`${prefix}-title`}>{spec.title}</h1><p>{spec.hint}</p></div><button className="secondary-button" disabled={blocked} onClick={() => setRefresh(n => n + 1)}>Tải lại</button></div>
    {onSectionChange && <nav className="shopping-section-nav" aria-label="Các mục mua sắm">{(Object.keys(definition) as ShoppingKind[]).map(k => <button key={k} className={kind === k ? 'primary-button' : 'secondary-button'} aria-current={kind === k ? 'page' : undefined} onClick={() => requestDirtyLeave(() => onSectionChange(k))}>{definition[k].title}</button>)}</nav>}
    {error && <p role="alert">{error}</p>}{error && selected && <button disabled={busy} onClick={() => { setSelected(undefined); onSelectRecord?.(); }}>Về danh sách</button>}{notice && <p role="status">{notice}</p>}{loading && <p role="status">Đang tải dữ liệu của bạn…</p>}
    {!loading && !allowed('read') && <p role="status">Chưa có quyền xem mục này trong phiên hiện tại.</p>}
    <div className="shopping-toolbar"><form onSubmit={e => { e.preventDefault(); if (!blocked) setFilter({ query, page: 1 }); }}><label htmlFor={`${prefix}-query`}>Tìm {spec.singular}</label><div className="shopping-search"><input id={`${prefix}-query`} maxLength={200} value={query} disabled={blocked || !allowed('read')} onChange={e => setQuery(e.target.value)} /><button disabled={blocked || !allowed('read')}>Tìm</button><button type="button" disabled={blocked || !allowed('read')} onClick={() => { setQuery(''); setFilter({ query: '', page: 1 }); }}>Xóa bộ lọc</button></div></form>
      {allowed('create') && <button className="primary-button" disabled={blocked} onClick={() => begin()}>Thêm {spec.singular}</button>}</div>
    {allowed('read') && !loading && !error && page?.items.length === 0 && <div className="shopping-empty"><h2>{filter.query ? 'Không tìm thấy kết quả' : 'Bắt đầu từ một bản ghi nhỏ'}</h2><p>{filter.query ? 'Thử từ khóa khác hoặc xóa bộ lọc.' : `Thêm ${spec.singular} đầu tiên để giữ thông tin gọn gàng và dễ tìm.`}</p></div>}
    {page && <div className="shopping-record-grid">{page.items.map(row => <article className="resource-card" key={row.id}><div><h2>{titleOf(row)}</h2>
      {'totalAmount' in row && <><p className="shopping-amount">{row.totalAmount} {row.currency}</p><p>{row.purchasedOn} · {row.status === 'Draft' ? 'Bản nháp' : row.status}</p>{row.sellerTitle && <p>{row.sellerTitle}</p>}</>}
      {'coverageKind' in row && <p>{row.coverageKind === 'Fixed' ? `Bảo hành đến ${row.endsOn}` : row.coverageKind === 'Lifetime' ? 'Trọn đời' : 'Chưa rõ thời hạn'}</p>}
      {'url' in row && row.url && <p className="shopping-url">{row.url}</p>}{'linkedOrderCount' in row && row.linkedOrderCount !== null && <p>{row.linkedOrderCount} đơn đã liên kết</p>}
      <button className="secondary-button" disabled={blocked} onClick={() => void open(row.id)}>Xem chi tiết</button></div></article>)}</div>}
    {page && <div className="shopping-pagination"><button disabled={blocked || page.page <= 1} onClick={() => setFilter(f => ({ ...f, page: f.page - 1 }))}>Trang trước</button><span>Trang {page.page}</span><button disabled={blocked || !page.hasMore} onClick={() => setFilter(f => ({ ...f, page: f.page + 1 }))}>Trang sau</button></div>}
    {detail && <article className="resource-card shopping-detail"><div><div className="content-heading"><h2>{titleOf(detail)}</h2><button disabled={blocked} onClick={() => { setSelected(undefined); onSelectRecord?.(); }}>Đóng chi tiết</button></div><RecordDetails record={detail} />
      {allowed('update') && (!('status' in detail) || detail.status === 'Draft') && <button className="primary-button" disabled={blocked} onClick={() => begin(detail)}>Chỉnh sửa</button>}
      {'status' in detail && <p className="shopping-help">Bản ghi thủ công. Trạng thái giao nhận, hoàn hàng và các tích hợp khác chưa được kích hoạt trong phần này.</p>}</div></article>}
    <ResourceFormDialog open={editor !== null} title={`${editor?.record ? 'Chỉnh sửa' : 'Thêm'} ${spec.singular}`} busy={busy} dirty={dirty && !uncertain} closeBlocked={uncertain}
      onClose={() => { setEditor(null); original.current = null; sourceSequence.current++; setSourceBusy(false); }}>
      <form className="resource-form shopping-editor" onSubmit={e => void persist(e)}>
        {formError && <p role="alert">{formError}</p>}
        {uncertain && <div className="shopping-recovery"><p role="status">Kết quả lưu chưa rõ. Bản nháp được giữ nguyên để bạn thử lại đúng yêu cầu.</p><button type="button" disabled={busy} onClick={() => setAbandon(true)}>Bỏ bản nháp và kiểm tra danh sách</button>{abandon && <div role="alert"><p>Bản ghi đã lưu trên máy chủ, nếu có, vẫn được giữ lại.</p><button type="button" disabled={busy} onClick={discardUnknown}>Xác nhận bỏ bản nháp</button><button type="button" disabled={busy} onClick={() => setAbandon(false)}>Tiếp tục khôi phục</button></div>}</div>}
        {conflictLocked && <div className="shopping-recovery"><h3>Phiên bản hiện tại</h3>{conflict ? <RecordDetails record={conflict} /> : <p>Chưa tải được phiên bản hiện tại.</p>}<div className="resource-actions"><button type="button" disabled={busy} onClick={() => void resolveConflict(false)}>Tải lại nội dung hiện tại</button><button type="button" disabled={busy || !conflict} onClick={() => void resolveConflict(true)}>Giữ bản nháp trên phiên bản mới</button></div></div>}
        {kind === 'sellers' && <>{field('Tên người bán', 'title', 200, true)}{field('Website công khai', 'url', 2048, false, 'url')}<p className="shopping-help">Website chỉ được lưu dưới dạng văn bản; không tự mở hoặc lấy dữ liệu từ bên ngoài.</p></>}
        {kind === 'orders' && <>
          <div className="shopping-form-grid">{field('Mã đơn (tùy chọn)', 'orderNumber')}{field('Ngày mua', 'purchasedOn', 10, true, 'date')}{field('Tiền tệ (ví dụ VND, USD)', 'currency', 3, true)}</div>
          <SourceSearch kind="sellers" query={sourceQuery} onQuery={setSourceQuery} onSearch={() => void sourceSearch()} onMore={() => void sourceSearch(true)} more={sourceMore && sourceQuery === sourceAppliedQuery} busy={sourceBusy} disabled={editLocked || !caps['shopping.seller.read']} error={sourceError} />
          <label>Người bán<select value={draft.sellerId} disabled={editLocked || !caps['shopping.seller.read']} onChange={e => change('sellerId', e.target.value)}><option value="">Không liên kết người bán</option>{draft.sellerId && !sources.some(s => s.id === draft.sellerId) && <option value={draft.sellerId}>Người bán đang liên kết</option>}{sources.map(s => <option key={s.id} value={s.id}>{titleOf(s)}</option>)}</select></label>
          {draft.sellerId && <button type="button" disabled={editLocked} onClick={() => change('sellerId', '')}>Gỡ liên kết người bán</button>}
          <fieldset disabled={editLocked}><legend>Sản phẩm trong đơn</legend>{draft.lines.map((line, index) => <div className="shopping-order-line" key={line.id ?? `new-${index}`}><h3>Sản phẩm {index + 1}</h3>{(['title', 'quantity', 'unitAmount', 'variant'] as const).map((name, i) => <label key={name}>{['Tên sản phẩm', 'Số lượng', 'Đơn giá', 'Phân loại (tùy chọn)'][i]}<input required={name !== 'variant'} inputMode={name === 'quantity' || name === 'unitAmount' ? 'decimal' : 'text'} maxLength={name === 'variant' ? 300 : name === 'title' ? 200 : 29} value={line[name]} onChange={e => change('lines', draft.lines.map((old, at) => at === index ? { ...old, [name]: e.target.value } : old))} /></label>)}<button type="button" disabled={draft.lines.length <= 1} onClick={() => change('lines', draft.lines.filter((_, at) => at !== index))}>Bỏ sản phẩm {index + 1}</button></div>)}<button type="button" disabled={draft.lines.length >= 100} onClick={() => change('lines', [...draft.lines, { id: null, title: '', quantity: '1', unitAmount: '', variant: '' }])}>Thêm sản phẩm</button></fieldset>
          <div className="shopping-form-grid">{field('Phí vận chuyển', 'shippingAmount', 29, true)}{field('Thuế', 'taxAmount', 29, true)}{field('Giảm giá', 'discountAmount', 29, true)}</div><p className="shopping-help">Dùng dấu chấm cho phần thập phân. Tổng tiền được tính chính xác khi lưu, không tự quy đổi tiền tệ.</p>
          <label>Ghi chú<textarea maxLength={20000} value={draft.notes} disabled={editLocked} onChange={e => change('notes', e.target.value)} /></label>
          {duplicate && <label><input type="checkbox" checked={draft.confirmDuplicateReference} disabled={editLocked} onChange={e => change('confirmDuplicateReference', e.target.checked)} />Tôi đã kiểm tra và muốn lưu thêm một đơn có mã này</label>}
        </>}
        {kind === 'warranties' && <>
          {field('Tên bảo hành', 'title', 200, true)}
          <SourceSearch kind="orders" query={sourceQuery} onQuery={setSourceQuery} onSearch={() => void sourceSearch()} onMore={() => void sourceSearch(true)} more={sourceMore && sourceQuery === sourceAppliedQuery} busy={sourceBusy} disabled={editLocked || !caps['shopping.order.read']} error={sourceError} />
          <label>Chọn đơn mua sắm<select disabled={editLocked || sourceBusy} value={sourceOrder?.id ?? ''} onChange={e => void chooseOrder(e.target.value)}><option value="">Chọn đơn để xem sản phẩm</option>{sources.map(s => <option key={s.id} value={s.id}>{titleOf(s)}</option>)}</select></label>
          <label>Sản phẩm được bảo hành<select required disabled={editLocked} value={draft.orderLineId} onChange={e => change('orderLineId', e.target.value)}><option value="">Chọn sản phẩm</option>{draft.orderLineId && !sourceOrder?.lines.some(l => l.id === draft.orderLineId) && <option value={draft.orderLineId}>Sản phẩm đang liên kết</option>}{sourceOrder?.lines.map(l => <option key={l.id} value={l.id}>{l.title}{l.variant ? ` · ${l.variant}` : ''}</option>)}</select></label>
          <label>Thời hạn<select value={draft.coverageKind} disabled={editLocked} onChange={e => change('coverageKind', e.target.value)}><option value="Unknown">Chưa rõ</option><option value="Fixed">Có thời hạn</option><option value="Lifetime">Trọn đời</option></select></label>
          {draft.coverageKind === 'Fixed' && <div className="shopping-form-grid">{field('Ngày bắt đầu', 'startsOn', 10, true, 'date')}{field('Ngày kết thúc', 'endsOn', 10, true, 'date')}</div>}{field('Đơn vị bảo hành', 'provider')}{field('Liên hệ yêu cầu bảo hành', 'claimContact', 1000)}<label>Điều khoản<textarea maxLength={20000} value={draft.terms} disabled={editLocked} onChange={e => change('terms', e.target.value)} /></label>
        </>}
        <button className="primary-button" disabled={busy || sourceBusy || conflictLocked || !allowed(editor?.record ? 'update' : 'create')}>{busy ? 'Đang lưu…' : uncertain ? 'Thử lại đúng yêu cầu lưu' : 'Lưu'}</button>
      </form>
    </ResourceFormDialog>
  </section>;
}

function RecordDetails({ record }: { record: ShoppingRecord }) {
  if ('coverageKind' in record) return <dl className="shopping-facts"><dt>Thời hạn</dt><dd>{record.coverageKind === 'Fixed' ? `${record.startsOn} → ${record.endsOn}` : record.coverageKind === 'Lifetime' ? 'Trọn đời' : 'Chưa rõ'}</dd><dt>Đơn vị bảo hành</dt><dd>{record.provider || 'Chưa ghi'}</dd><dt>Liên hệ</dt><dd>{record.claimContact || 'Chưa ghi'}</dd><dt>Điều khoản</dt><dd className="shopping-private-text">{record.terms || 'Chưa ghi'}</dd></dl>;
  if ('lines' in record) return <><dl className="shopping-facts"><dt>Người bán lúc ghi đơn</dt><dd>{record.sellerTitle || 'Không liên kết'}</dd><dt>Ngày mua</dt><dd>{record.purchasedOn}</dd><dt>Trạng thái</dt><dd>{record.status === 'Draft' ? 'Bản nháp' : record.status}</dd><dt>Tổng tiền</dt><dd>{record.totalAmount} {record.currency}</dd><dt>Vận chuyển / Thuế / Giảm giá</dt><dd>{record.shippingAmount} / {record.taxAmount} / {record.discountAmount} {record.currency}</dd><dt>Ghi chú</dt><dd className="shopping-private-text">{record.notes || 'Chưa ghi'}</dd></dl><ul>{record.lines.map(l => <li key={l.id}>{l.title}{l.variant ? ` · ${l.variant}` : ''} — {l.quantity} × {l.unitAmount} {record.currency}</li>)}</ul></>;
  return <p className="shopping-url">{record.url || 'Chưa thêm website.'}</p>;
}
function SourceSearch({ kind, query, onQuery, onSearch, onMore, more, busy, disabled, error }: {
  kind: 'sellers' | 'orders'; query: string; onQuery: (q: string) => void; onSearch: () => void; onMore: () => void;
  more: boolean; busy: boolean; disabled: boolean; error: string | null;
}) {
  return <div className="shopping-source-search"><label>{kind === 'sellers' ? 'Tìm tên người bán' : 'Tìm mã đơn hoặc người bán'}<input value={query} maxLength={200} disabled={disabled || busy} onChange={e => onQuery(e.target.value)} /></label><button type="button" disabled={disabled || busy} onClick={onSearch}>{busy ? 'Đang tìm…' : 'Tìm để chọn'}</button>{more && <button type="button" disabled={disabled || busy} onClick={onMore}>Xem thêm kết quả</button>}{error && <p role="alert">{error}</p>}</div>;
}
