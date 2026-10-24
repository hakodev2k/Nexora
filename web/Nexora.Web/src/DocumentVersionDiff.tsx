import { useMemo } from 'react';
import type { DocumentVersionPreview } from './api';
import { compareDocumentText } from './documentTextDiff';

const sourceStyle = { whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', maxWidth: '100%' } as const;
export function DocumentVersionDiff({ before, after }: { before: DocumentVersionPreview; after: DocumentVersionPreview }) {
  const comparison = useMemo(() => compareDocumentText(before.body, after.body), [before.body, after.body]);
  const fields = [
    ['Tiêu đề', before.snapshot.title, after.snapshot.title],
    ['Loại', before.snapshot.documentType, after.snapshot.documentType],
    ['Chế độ soạn thảo', before.snapshot.editorMode, after.snapshot.editorMode],
    ['Trạng thái khi lưu', before.snapshot.savedStatus, after.snapshot.savedStatus],
    ['Tag', before.snapshot.tagSnapshotCaptured ? before.snapshot.tagName ?? 'Không có Tag' : 'Chưa ghi nhận', after.snapshot.tagSnapshotCaptured ? after.snapshot.tagName ?? 'Không có Tag' : 'Chưa ghi nhận'],
    ['Ghi chú thay đổi', before.snapshot.changeNote ?? 'Không có', after.snapshot.changeNote ?? 'Không có']
  ].filter(([, oldValue, newValue]) => oldValue !== newValue);
  const identical = before.body === after.body;
  return <section aria-label="Khác biệt hai phiên bản" className="form-panel">
    <h3>v{before.snapshot.versionNumber} → v{after.snapshot.versionNumber}</h3>
    {fields.length > 0 ? <dl>{fields.map(([name, oldValue, newValue]) => <div key={name}><dt>{name}</dt><dd><span>Trước: {oldValue}</span><br /><span>Sau: {newValue}</span></dd></div>)}</dl> : <p>Thông tin trang không thay đổi.</p>}
    {identical ? <p>Nội dung hai phiên bản giống nhau.</p> : <>
      <p>{comparison.simplified ? 'Nội dung lớn: hiển thị phần thay thế đầy đủ, không tính diff từng dòng.' : 'So sánh theo dòng; giữ nguyên ký tự và xuống dòng của source.'}</p>
      {comparison.changes.map((change, index) => <article key={index} aria-label={change.kind === 'same' ? 'Giữ nguyên' : change.kind === 'removed' ? 'Đã bỏ' : 'Đã thêm'}>
        <h4>{change.kind === 'same' ? 'Giữ nguyên' : change.kind === 'removed' ? '− Đã bỏ' : '+ Đã thêm'}</h4>
        <pre className="conflict-preview" style={sourceStyle}>{change.text}</pre>
      </article>)}
    </>}
    <details><summary>Xem toàn bộ hai snapshot</summary><div className="resource-layout">{[before, after].map((version, index) => <article key={index} className="conflict-panel" style={{ minWidth: 0 }}><h4>v{version.snapshot.versionNumber} — {version.snapshot.title}</h4><pre style={sourceStyle}>{version.body}</pre></article>)}</div></details>
  </section>;
}
