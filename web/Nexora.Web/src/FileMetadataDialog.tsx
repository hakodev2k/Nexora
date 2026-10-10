import { useEffect, useRef, useState } from 'react';
import { apiFetch, getFileCapabilities, type FileRecord } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';

export function FileMetadataDialog({ fileId, onClose, onDenied }: {
  fileId: string; onClose: () => void; onDenied: () => void;
}) {
  const generation = useRef(0);
  const [record, setRecord] = useState<FileRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [reload, setReload] = useState(0);
  useEffect(() => {
    const stamp = ++generation.current;
    setRecord(null); setLoading(true); setError('');
    void Promise.all([
      apiFetch<FileRecord>(`/api/v1/files/${encodeURIComponent(fileId)}`),
      getFileCapabilities()
    ]).then(([value, caps]) => {
      if (stamp !== generation.current) return;
      if (caps['files.file.read'] !== true) { onDenied(); return; }
      if (value.id !== fileId || typeof value.originalName !== 'string' ||
          typeof value.mediaType !== 'string' || !Number.isSafeInteger(value.byteLength) ||
          value.byteLength < 0 || !['Pending', 'Clean', 'Quarantined', 'Failed'].includes(value.scanState) ||
          !['Active', 'Trash'].includes(value.lifecycle) || !Number.isSafeInteger(value.currentRevision) ||
          value.currentRevision < 1 || typeof value.etag !== 'string' ||
          typeof value.createdAt !== 'string' || typeof value.updatedAt !== 'string' ||
          !Number.isFinite(Date.parse(value.createdAt)) || !Number.isFinite(Date.parse(value.updatedAt))) {
        setError('Không thể xác nhận metadata file. Hãy tải lại.'); return;
      }
      setRecord(value);
    }).catch(e => {
      if (stamp !== generation.current) return;
      const failure = e as { status?: number; code?: string };
      if ([401, 403, 404].includes(failure.status ?? 0) || failure.code === 'ModuleUnavailable') {
        onDenied(); return;
      }
      setError('Không thể tải metadata file. Hãy thử lại.');
    }).finally(() => { if (stamp === generation.current) setLoading(false); });
    return () => { generation.current++; };
  }, [fileId, reload]);
  const date = (value: string) => new Date(value).toLocaleString('vi-VN');
  return <ResourceFormDialog open title="Chi tiết file" busy={false} dirty={false} onClose={onClose}>
    {loading && <p role="status">Đang tải metadata file hiện tại…</p>}
    {error && <p role="alert">{error}</p>}
    {record && <dl>
      <dt>Tên file</dt><dd>{record.originalName}</dd>
      <dt>Loại</dt><dd>{record.mediaType}</dd>
      <dt>Kích thước</dt><dd>{record.byteLength.toLocaleString('vi-VN')} bytes</dd>
      <dt>Scan</dt><dd>{record.scanState}</dd>
      <dt>Lifecycle</dt><dd>{record.lifecycle}</dd>
      <dt>Revision</dt><dd>{record.currentRevision}</dd>
      <dt>Tạo lúc</dt><dd>{date(record.createdAt)}</dd>
      <dt>Cập nhật lúc</dt><dd>{date(record.updatedAt)}</dd>
    </dl>}
    <p className="field-help">Đây là metadata; nội dung và tham chiếu không được tải trong dialog này.</p>
    <button type="button" className="secondary-button" disabled={loading}
      onClick={() => { setRecord(null); setReload(value => value + 1); }}>Tải lại metadata</button>
  </ResourceFormDialog>;
}
