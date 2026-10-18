import { useEffect, useRef, useState } from 'react';
import { apiFetch, isProductivityAcknowledgement, jsonMutationHeaders, listFiles, NexoraApiError, type FileRecord } from './api';
import { documentCapabilities } from './documentTopologyApi';
import { DocumentBodyView } from './DocumentBodyEditor';

export type DocumentImportDraft = { fileId: string; fileETag: string; editorMode: string; body: string; previewToken: string; warnings: string[]; expiresAt: string };
type Preview = DocumentImportDraft & { parserVersion: number };
export async function commitDocumentImport(source: DocumentImportDraft, page: { title: string; body: string; documentType: string; editorMode: string; tagId: string | null; folderId?: string | null; parentPageId?: string | null }, key: string) {
  const result = await apiFetch<unknown>('/api/v1/document-imports', { method: 'POST', headers: jsonMutationHeaders(key), body: JSON.stringify({ fileId: source.fileId, fileETag: source.fileETag, previewToken: source.previewToken, confirmed: true, acceptedWarnings: source.warnings, draftEdited: page.body !== source.body, page }) }, 201);
  if (!isProductivityAcknowledgement(result) || typeof result.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(result.id)) throw new NexoraApiError('Chưa xác nhận được import. Giữ nguyên yêu cầu/key để đối chiếu.', 0, 'UnexpectedAcknowledgement');
  return result;
}

export function DocumentImportReceiptNotice({ pageId, onUnavailable, onAuthLost }: { pageId: string; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [report, setReport] = useState<{ editorMode: string; createdAt: string } | null>(null), [error, setError] = useState(''), [busy, setBusy] = useState(false); const epoch = useRef(0), flight = useRef(false);
  useEffect(() => () => { epoch.current++; }, [pageId]);
  async function read() {
    if (flight.current) return; const stamp = ++epoch.current; flight.current = true; setBusy(true); setReport(null); setError('');
    try {
      const value = await apiFetch<{ pageId: string; state: string; parserVersion: number; editorMode: string; createdAt: string }>(`/api/v1/document-imports/by-page/${encodeURIComponent(pageId)}`); if (stamp !== epoch.current) return;
      if (!value || value.pageId !== pageId || value.state !== 'Committed' || value.parserVersion !== 1 || !['Markdown', 'Block'].includes(value.editorMode) || typeof value.createdAt !== 'string') throw new NexoraApiError('Report import không hợp lệ.', 0, 'UnexpectedResponse'); setReport(value);
    } catch (value) { if (stamp !== epoch.current) return; setError(value instanceof Error ? value.message : 'Report không khả dụng.'); const status = (value as { status?: number }).status; if ([401, 403, 404].includes(status ?? 0)) { onUnavailable(); if (status === 401) void onAuthLost(); } }
    finally { if (stamp === epoch.current) { flight.current = false; setBusy(false); } }
  }
  return <section className="nested-panel"><h3>Import đã lưu</h3><p>Không gửi lại yêu cầu. Report chỉ đọc metadata của lần import, không tải lại file hoặc nội dung page.</p><button type="button" disabled={busy} onClick={() => void read()}>Xem report với quyền hiện tại</button>{error && <p role="alert">{error}</p>}{report && <p>Đã tạo một trang · {report.editorMode} · {report.createdAt}</p>}</section>;
}
const warnings: Record<string, string> = {
  WordLayoutStylesNormalized: 'Chỉ nhập nội dung cơ bản; font, màu, spacing và bố cục Word không được giữ nguyên.',
  ImagesOmitted: 'Ảnh bị bỏ khỏi nội dung chuyển đổi.', ExternalRelationshipsOmitted: 'Không mở hoặc sao chép relationship ngoài; chỉ giữ văn bản được hỗ trợ.',
  CommentsOmitted: 'Không nhập comments.', FootnotesOmitted: 'Không nhập footnote/endnote.', EmbeddedObjectsOmitted: 'Không nhập embedded objects.',
  PageLayoutOmitted: 'Không giữ layout/phân trang Word.', FormattingStylesFlattened: 'Style chưa hỗ trợ được làm phẳng.', InlineFormattingFlattened: 'Định dạng inline được chuyển thành văn bản trong Block.',
  HeadingDepthFlattened: 'Cấp heading được chuẩn hóa theo editor đích.', ListNumberStyleNormalized: 'Kiểu đánh số được chuẩn hóa.', ListStructureFlattened: 'Cấu trúc danh sách chưa hỗ trợ được làm phẳng.',
  NestedListStructureFlattened: 'Danh sách lồng nhau được làm phẳng.', NumberedHeadingFlattened: 'Heading có số được chuẩn hóa.', TabsExpanded: 'Tab được thay bằng khoảng trắng.',
  UnsupportedInlineStructureFlattened: 'Cấu trúc inline chưa hỗ trợ được làm phẳng.', UnsupportedDocumentStructureFlattened: 'Cấu trúc tài liệu chưa hỗ trợ được làm phẳng.',
  TableFirstRowBecomesHeader: 'Hàng đầu bảng thành header Markdown.', MergedCellsFlattened: 'Merge ô được làm phẳng.', NestedTablesOmitted: 'Không nhập bảng lồng trong ô.',
  TableCellBlocksFlattened: 'Các đoạn trong ô được nối thành văn bản.', TableCellLineBreaksFlattened: 'Xuống dòng trong ô được làm phẳng.', TableColumnsNormalized: 'Số cột bảng được chuẩn hóa.',
  TableHeaderFormattingFlattened: 'Định dạng header bảng được làm phẳng.', TableLayoutNormalized: 'Bố cục bảng được chuẩn hóa.', EquationsOmitted: 'Không nhập equations.',
  WhitespaceFormattingNormalized: 'Định dạng ở khoảng trắng đầu/cuối run được chuẩn hóa.', HtmlShownLiterally: 'HTML được giữ như văn bản, không thực thi.',
  LinkTargetsOmitted: 'Đích liên kết bị bỏ; giữ văn bản.', NestedStructureFlattened: 'Cấu trúc lồng được làm phẳng.', QuoteStructureFlattened: 'Quote được chuẩn hóa thành văn bản.',
  MixedChecklistFlattened: 'Checklist trộn với list thường được làm phẳng.', OrderedListStartNormalized: 'Số bắt đầu danh sách được chuẩn hóa.', CodeLanguageOmitted: 'Bỏ nhãn ngôn ngữ code chưa hỗ trợ.',
  UnsupportedInlineFlattened: 'Inline chưa hỗ trợ được làm phẳng.', UnsupportedInlineOmitted: 'Bỏ inline chưa hỗ trợ.', UnsupportedBlockShownLiterally: 'Block chưa hỗ trợ được giữ như văn bản.',
  UnsupportedContainerFlattened: 'Container chưa hỗ trợ được làm phẳng.', UnsupportedBlockOmitted: 'Bỏ block chưa hỗ trợ.', ImageReferencesNotEmbedded: 'Markdown giữ tham chiếu ảnh dạng source, không nhúng hoặc tải ảnh ngoài.',
  LinksRemainLiteralSource: 'Markdown giữ cú pháp liên kết dạng source; Nexora không mở liên kết ngoài.', HtmlRemainsLiteralSource: 'Markdown giữ HTML dạng source; Nexora không thực thi HTML.'
};

export function DocumentImportPanel({ mode, disabled, source, onWorkingChange, onApply, onClear, onUnavailable, onAuthLost }: { mode: string; disabled: boolean; source: DocumentImportDraft | null; onWorkingChange: (value: boolean) => void; onApply: (source: DocumentImportDraft, replaceBody: boolean) => void; onClear: () => void; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [open, setOpen] = useState(false), [kind, setKind] = useState<'' | 'MD' | 'DOCX'>(''), [query, setQuery] = useState('');
  const [items, setItems] = useState<FileRecord[]>([]), [next, setNext] = useState<string | null>(null), [selected, setSelected] = useState<FileRecord | null>(null), [preview, setPreview] = useState<Preview | null>(null);
  const [accepted, setAccepted] = useState<string[]>([]), [choice, setChoice] = useState<'' | 'replace' | 'keep'>(''), [busy, setBusy] = useState(false), [error, setError] = useState('');
  const epoch = useRef(0), flight = useRef(false), callbacks = useRef({ onWorkingChange, onApply, onUnavailable, onAuthLost }); callbacks.current = { onWorkingChange, onApply, onUnavailable, onAuthLost };
  useEffect(() => { let cancelled = false; setCaps({}); documentCapabilities().then(value => { if (!cancelled) setCaps(value); }).catch(value => { if (!cancelled && value.status === 401) void callbacks.current.onAuthLost(); }); return () => { cancelled = true; }; }, [mode]);
  useEffect(() => () => { epoch.current++; callbacks.current.onWorkingChange(false); }, []);
  const allowed = ['documents.format.import', 'documents.page.create', 'transfer.import.preview', 'transfer.import.commit', 'files.file.read', 'files.file.download'].every(action => caps[action]);
  function close() { epoch.current++; flight.current = false; setOpen(false); setBusy(false); setItems([]); setNext(null); setSelected(null); setPreview(null); setAccepted([]); setChoice(''); setError(''); callbacks.current.onWorkingChange(false); }
  function failure(value: unknown) { const result = value as { status?: number; message?: string }; if ([401, 403, 404].includes(result.status ?? 0)) { close(); callbacks.current.onUnavailable(); if (result.status === 401) void callbacks.current.onAuthLost(); } else setError(result.message ?? 'Không thể đọc nguồn import.'); }
  async function load(cursor = '') {
    if (flight.current || disabled || !kind) return; const stamp = ++epoch.current; flight.current = true; setBusy(true); setError(''); setItems([]); setSelected(null); setPreview(null); setAccepted([]); setNext(null);
    try { const page = await listFiles(25, { cursor, query, mediaType: kind === 'MD' ? 'text/markdown' : 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', scanState: 'Clean', lifecycle: 'Active' }); if (stamp === epoch.current) { setItems(page.items); setNext(page.nextCursor); } }
    catch (value) { if (stamp === epoch.current) failure(value); } finally { if (stamp === epoch.current) { flight.current = false; setBusy(false); } }
  }
  async function convert() {
    if (flight.current || disabled || !selected) return; const file = selected, stamp = ++epoch.current; flight.current = true; setBusy(true); setError(''); setPreview(null); setAccepted([]); setChoice('');
    try { const value = await apiFetch<Preview>('/api/v1/document-imports/preview', { method: 'POST', headers: jsonMutationHeaders(), body: JSON.stringify({ fileId: file.id, fileETag: file.etag, editorMode: mode }) }); if (stamp !== epoch.current) return;
      if (!value || value.fileId !== file.id || value.fileETag !== file.etag || value.editorMode !== mode || value.parserVersion !== 1 || typeof value.body !== 'string' || value.body.includes('\0') || new TextEncoder().encode(value.body).length > 1048576 || typeof value.previewToken !== 'string' || value.previewToken.length > 1024 || !Array.isArray(value.warnings) || value.warnings.length > 64 || value.warnings.some(code => typeof code !== 'string' || !/^[A-Za-z][A-Za-z0-9]{0,63}$/.test(code)) || new Set(value.warnings).size !== value.warnings.length) throw new NexoraApiError('Preview import không hợp lệ.', 0, 'UnexpectedResponse');
      setPreview(value);
    } catch (value) { if (stamp === epoch.current) failure(value); } finally { if (stamp === epoch.current) { flight.current = false; setBusy(false); } }
  }
  return <section className="nested-panel" aria-label="Import trang mới"><h3>Import trang mới từ Files</h3><p>Chọn Type và EditorMode trước. Chỉ file sạch của bạn; preview chưa tạo page. Apply chỉ đổi draft; bấm Save mới import.</p>
    {source && <p>Draft có nguồn import · {source.editorMode} · preview hết hạn {source.expiresAt}. <button type="button" disabled={disabled || open} onClick={onClear}>Bỏ import, giữ nội dung như draft thủ công</button></p>}
    {!open ? <button type="button" disabled={disabled || !allowed || !['Block', 'Markdown'].includes(mode)} onClick={() => { setOpen(true); callbacks.current.onWorkingChange(true); }}>Chọn / xem lại file nguồn</button> : <>
      <button type="button" onClick={close}>Hủy chọn nguồn, giữ draft hiện tại</button>{busy && <p role="status">Đang đọc nguồn…</p>}{error && <p role="alert">{error}</p>}
      <label>Loại file<select value={kind} disabled={busy || disabled} onChange={event => { epoch.current++; setKind(event.target.value as typeof kind); setItems([]); setSelected(null); setPreview(null); setNext(null); }}><option value="">Chọn format nguồn</option><option value="MD">.md</option><option value="DOCX">.docx</option></select></label>
      <label>Tìm tên file<input value={query} maxLength={200} disabled={busy || disabled} onChange={event => { epoch.current++; setQuery(event.target.value); setItems([]); setSelected(null); setPreview(null); setNext(null); }} /></label><button type="button" disabled={busy || disabled || !kind} onClick={() => void load()}>Tải Files sạch</button>
      <ul>{items.map(file => <li key={file.id}><button type="button" disabled={busy || disabled} onClick={() => { setSelected(file); setPreview(null); setAccepted([]); setChoice(''); }}>{file.originalName} · {file.byteLength} byte</button></li>)}</ul>{next && <button type="button" disabled={busy || disabled} onClick={() => void load(next)}>Trang Files tiếp</button>}
      {selected && <p>Đã chọn: {selected.originalName} <button type="button" disabled={busy || disabled} onClick={() => void convert()}>Preview chuyển đổi</button></p>}
      {preview && <><p>Preview hết hạn {preview.expiresAt}. Source file không đổi. Nội dung không hỗ trợ có thể bị bỏ hoặc làm phẳng.</p><DocumentBodyView body={preview.body} mode={mode} />{preview.warnings.map(code => <label key={code}><input type="checkbox" disabled={busy || disabled} checked={accepted.includes(code)} onChange={event => setAccepted(values => event.target.checked ? [...values, code] : values.filter(value => value !== code))} />{warnings[code] ?? `Chuyển đổi cần chấp nhận: ${code}`}</label>)}
        <label>Cách áp dụng<select value={choice} disabled={busy || disabled} onChange={event => setChoice(event.target.value as typeof choice)}><option value="">Chọn rõ cách áp dụng</option><option value="replace">Thay body draft bằng nội dung chuyển đổi</option><option value="keep">Giữ body draft đã chỉnh sửa, ghi nhận là bản đã sửa</option></select></label>
        <button type="button" disabled={busy || disabled || !choice || accepted.length !== preview.warnings.length} onClick={() => { const draft: DocumentImportDraft = { fileId: preview.fileId, fileETag: preview.fileETag, editorMode: preview.editorMode, body: preview.body, previewToken: preview.previewToken, warnings: [...accepted].sort(), expiresAt: preview.expiresAt }; close(); callbacks.current.onApply(draft, choice === 'replace'); }}>Apply vào draft — chưa Save</button></>}
    </>}
  </section>;
}
