import { useEffect, useRef, useState } from 'react';
import { apiFetch, createIdempotencyKey, documentExportBlob, isProductivityAcknowledgement, jsonMutationHeaders, NexoraApiError } from './api';
import { documentCapabilities } from './documentTopologyApi';
import { ResourceFormDialog } from './ResourceFormDialog';

type Target = { id: string; etag: string; versionNumber: number };
type Preview = { pageId: string; versionNumber: number; format: 'MD' | 'DOCX'; etag: string; warnings: string[]; previewToken: string; expiresAt: string; byteLength: number };
type Job = { id: string; pageId: string; versionNumber: number; format: 'MD' | 'DOCX'; codecVersion: number; state: 'Ready' | 'Expired'; createdAt: string; expiresAt: string; etag: string };
const warningText: Record<string, string> = {
  LegacyBlockText: 'Block cũ được xuất dưới dạng văn bản.', ExternalLinksInert: 'Liên kết ngoài được giữ như văn bản, không tạo liên kết điều hướng.',
  CodeLanguageOmitted: 'Bỏ nhãn ngôn ngữ code không tương thích.', TableFirstRowBecomesHeader: 'Hàng đầu bảng trở thành header trong Markdown.',
  TableCellLineBreaksFlattened: 'Xuống dòng trong ô bảng được thay bằng khoảng trắng.', XmlCharactersOmitted: 'Bỏ ký tự không hợp lệ trong XML.',
  HtmlShownLiterally: 'HTML được giữ dưới dạng văn bản.', ImagesOmitted: 'Ảnh bị bỏ khỏi artifact; nội dung nguồn không đổi.',
  ChecklistIsStatic: 'Checklist được xuất thành ký hiệu tĩnh.', UnsupportedInlineFlattened: 'Định dạng inline chưa hỗ trợ được làm phẳng.',
  ImageReferencesNotEmbedded: 'Markdown giữ nguyên tham chiếu ảnh dạng source; không nhúng, kiểm tra hoặc tải ảnh ngoài.', HtmlRemainsLiteralSource: 'Markdown giữ nguyên HTML dạng source; Nexora không thực thi HTML.', TableLayoutNormalized: 'Bố cục và căn lề bảng được chuẩn hóa sang bảng Word cơ bản.',
  LinksRemainLiteralSource: 'Markdown giữ nguyên cú pháp liên kết dạng source; Nexora không mở liên kết ngoài.', NestedQuoteIndentFlattened: 'Độ thụt lề quote lồng nhau được làm phẳng trong Word.',
  OrderedListStartNormalized: 'Số bắt đầu danh sách không tương thích được chuẩn hóa về 1.',
  EmptyBlockSpacingNormalized: 'Khoảng cách của paragraph trống được chuẩn hóa.', CodeTrailingNewlineAdded: 'Code được thêm xuống dòng cuối để tạo fenced code Markdown hợp lệ.',
  UnsupportedInlineOmitted: 'Bỏ inline chưa hỗ trợ.', DeepListIndentFlattened: 'Độ sâu danh sách vượt giới hạn Word được làm phẳng.',
  UnsupportedBlockShownLiterally: 'Block chưa hỗ trợ được giữ như văn bản.', UnsupportedContainerFlattened: 'Container chưa hỗ trợ được làm phẳng.',
  UnsupportedBlockOmitted: 'Bỏ block chưa hỗ trợ.'
};

export function DocumentExportLauncher({ target, disabled, historical = false, onUnavailable, onAuthLost }: { target: Target; disabled: boolean; historical?: boolean; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [open, setOpen] = useState(false);
  const auth = useRef(onAuthLost); auth.current = onAuthLost;
  useEffect(() => { let cancelled = false; setCaps({}); documentCapabilities().then(value => { if (!cancelled) setCaps(value); }).catch(value => { if (!cancelled && value.status === 401) void auth.current(); }); return () => { cancelled = true; }; }, [target.id, target.etag]);
  const allowed = caps['documents.format.export'] && caps['documents.page.read'] && caps['transfer.export.request'] && (!historical || caps['documents.page.history']);
  return <><button type="button" className="secondary-button" disabled={disabled || !allowed} onClick={() => setOpen(true)}>Export bản đã lưu v{target.versionNumber}</button>{open && <DocumentExportDialog target={target} caps={caps} onClose={() => setOpen(false)} onUnavailable={onUnavailable} onAuthLost={onAuthLost} />}</>;
}

function DocumentExportDialog({ target, caps, onClose, onUnavailable, onAuthLost }: { target: Target; caps: Record<string, boolean>; onClose: () => void; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [format, setFormat] = useState<'' | 'MD' | 'DOCX'>(''), [preview, setPreview] = useState<Preview | null>(null), [confirmed, setConfirmed] = useState(false), [accepted, setAccepted] = useState<string[]>([]);
  const [jobId, setJobId] = useState(''), [job, setJob] = useState<Job | null>(null), [busy, setBusy] = useState(false), [error, setError] = useState(''), [notice, setNotice] = useState('');
  const [intent, setIntent] = useState<{ target: Target; preview: Preview; accepted: string[]; key: string } | null>(null);
  const epoch = useRef(0), flight = useRef(false);
  useEffect(() => () => { epoch.current++; }, []);
  function fail(value: unknown) {
    const failure = value as { message?: string; status?: number }; setError(failure.message ?? 'Thao tác chưa xác nhận.');
    if ([401, 403, 404].includes(failure.status ?? 0)) { setPreview(null); setJob(null); setAccepted([]); setConfirmed(false); onUnavailable(); if (failure.status === 401) void onAuthLost(); }
  }
  async function readPreview() {
    if (flight.current || intent || !format || jobId) return; const stamp = ++epoch.current; flight.current = true; setBusy(true); setError(''); setPreview(null); setConfirmed(false); setAccepted([]);
    try {
      const value = await apiFetch<Preview>(`/api/v1/documents/${encodeURIComponent(target.id)}/exports/preview`, { method: 'POST', headers: jsonMutationHeaders(undefined, { 'If-Match': target.etag }), body: JSON.stringify({ versionNumber: target.versionNumber, format }) });
      if (stamp !== epoch.current) return;
      if (!value || value.pageId !== target.id || value.versionNumber !== target.versionNumber || value.format !== format || value.etag !== target.etag || !Array.isArray(value.warnings) || value.warnings.some(code => typeof code !== 'string') || typeof value.previewToken !== 'string' || !Number.isSafeInteger(value.byteLength) || value.byteLength < 0 || value.byteLength > 5242880)
        throw new NexoraApiError('Chưa xác nhận được preview.', 0, 'UnexpectedResponse');
      setPreview(value);
    } catch (value) { if (stamp === epoch.current) fail(value); }
    finally { flight.current = false; if (stamp === epoch.current) setBusy(false); }
  }
  async function generate() {
    if (flight.current || (!intent && (!preview || !confirmed || accepted.length !== preview.warnings.length))) return;
    const command = intent ?? { target: { ...target }, preview: preview!, accepted: [...accepted].sort(), key: createIdempotencyKey() }; const stamp = ++epoch.current; flight.current = true; setBusy(true); setIntent(command); setError('');
    try {
      const value = await apiFetch<unknown>(`/api/v1/documents/${encodeURIComponent(command.target.id)}/exports`, { method: 'POST', headers: jsonMutationHeaders(command.key, { 'If-Match': command.target.etag }), body: JSON.stringify({ versionNumber: command.target.versionNumber, format: command.preview.format, previewToken: command.preview.previewToken, confirmed: true, acceptedWarnings: command.accepted }) }, 201);
      if (stamp !== epoch.current) return;
      if (!isProductivityAcknowledgement(value) || typeof value.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.id)) throw new NexoraApiError('Chưa xác nhận được job. Giữ nguyên yêu cầu để đối chiếu.', 0, 'UnexpectedAcknowledgement');
      setJobId(value.id); setIntent(null); setPreview(null); setAccepted([]); setConfirmed(false); setNotice('Đã tạo artifact riêng. Không gửi lại yêu cầu; xem trạng thái hoặc tải bằng quyền hiện tại.');
    } catch (value) {
      if (stamp !== epoch.current) return; const failure = value as { status?: number; code?: string };
      if (failure.status && failure.status < 500 && !['RequestInProgress', 'IdempotencyConflict', 'RetryableConflict'].includes(failure.code ?? '')) { setIntent(null); setPreview(null); setAccepted([]); setConfirmed(false); }
      fail(value);
    } finally { flight.current = false; if (stamp === epoch.current) setBusy(false); }
  }
  async function status() {
    if (flight.current || !jobId) return; const stamp = ++epoch.current; flight.current = true; setBusy(true); setError(''); setJob(null);
    try { const value = await apiFetch<Job>(`/api/v1/document-exports/${encodeURIComponent(jobId)}`); if (stamp !== epoch.current) return;
      if (!value || value.id !== jobId || value.pageId !== target.id || value.versionNumber !== target.versionNumber || value.format !== format || value.codecVersion !== 1 || !['Ready', 'Expired'].includes(value.state)) throw new NexoraApiError('Trạng thái job không hợp lệ.', 0, 'UnexpectedResponse'); setJob(value);
    } catch (value) { if (stamp === epoch.current) fail(value); } finally { flight.current = false; if (stamp === epoch.current) setBusy(false); }
  }
  async function download() {
    if (flight.current || !jobId || !format) return; const stamp = ++epoch.current; flight.current = true; setBusy(true); setError('');
    try { const blob = await documentExportBlob(jobId, format); if (stamp !== epoch.current) return; const url = URL.createObjectURL(blob), link = document.createElement('a'); link.href = url; link.download = `document-v${target.versionNumber}.${format.toLowerCase()}`; link.click(); queueMicrotask(() => URL.revokeObjectURL(url)); setNotice('Đã nhận artifact. Trình duyệt xử lý lưu file.'); }
    catch (value) { if (stamp === epoch.current) { setJob(null); fail(value); } } finally { flight.current = false; if (stamp === epoch.current) setBusy(false); }
  }
  return <ResourceFormDialog open title={`Export bản đã lưu v${target.versionNumber}`} busy={busy} dirty={intent !== null || (!jobId && Boolean(format || confirmed))} onClose={onClose}>
    <p>Chỉ xuất một phiên bản đã lưu. Không xuất draft chưa lưu, children, history bundle, reminder, audit hoặc share token. File riêng hết hạn sau 15 phút; quyền và source được kiểm tra lại khi tải.</p>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {!jobId && <><label>Format<select value={format} disabled={busy || intent !== null} onChange={event => { setFormat(event.target.value as typeof format); setPreview(null); setConfirmed(false); setAccepted([]); }}><option value="">Chọn format</option><option value="MD">Markdown (.md)</option><option value="DOCX">Word (.docx)</option></select></label><button type="button" disabled={busy || intent !== null || !format} onClick={() => void readPreview()}>Xem trước chuyển đổi</button></>}
    {preview && <><p>{preview.byteLength.toLocaleString()} byte · preview hết hạn {preview.expiresAt}</p>{preview.warnings.length === 0 ? <p>Không phát hiện omission trong subset được hỗ trợ. Fidelity vẫn cần kiểm tra file.</p> : preview.warnings.map(code => <label key={code}><input type="checkbox" disabled={busy || intent !== null} checked={accepted.includes(code)} onChange={event => setAccepted(values => event.target.checked ? [...values, code] : values.filter(value => value !== code))} />{warningText[code] ?? `Chuyển đổi cần chấp nhận: ${code}`}</label>)}<label><input type="checkbox" disabled={busy || intent !== null} checked={confirmed} onChange={event => setConfirmed(event.target.checked)} /> Tôi xác nhận phiên bản, format và các điểm mất chi tiết đã nêu.</label></>}
    {intent && <p role="alert">Kết quả chưa rõ. Đối chiếu cùng yêu cầu/key; bỏ yêu cầu không hoàn tác job đã tạo.</p>}
    {!jobId && <button type="button" className="primary-button" disabled={busy || (!intent && (!preview || !confirmed || accepted.length !== preview.warnings.length))} onClick={() => void generate()}>{intent ? 'Đối chiếu cùng yêu cầu' : 'Tạo artifact'}</button>}
    {jobId && <div><p>Job: {jobId}</p>{job && <p>{job.state} · hết hạn {job.expiresAt}</p>}{caps['transfer.export.read'] && <button type="button" disabled={busy} onClick={() => void status()}>Xem trạng thái hiện tại</button>}{caps['transfer.export.download'] && <button type="button" disabled={busy || job?.state === 'Expired'} onClick={() => void download()}>Tải file với quyền hiện tại</button>}</div>}
  </ResourceFormDialog>;
}
