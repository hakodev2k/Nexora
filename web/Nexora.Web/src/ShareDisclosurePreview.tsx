import { useEffect, useRef, useState, type ReactNode } from 'react';
import { NexoraApiError, previewShareDisclosure, type SharedResource } from './api';

export function ShareDisclosurePreview({ resourceType, resourceId, disabled, onReady, render }: {
  resourceType: string; resourceId: string; disabled: boolean;
  onReady: (identity: string | null) => void; render: (value: SharedResource) => ReactNode;
}) {
  const [resource, setResource] = useState<SharedResource | null>(null);
  const [loadedIdentity, setLoadedIdentity] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const generation = useRef(0);
  const identity = `${resourceType}:${resourceId}`;
  useEffect(() => {
    generation.current++; setResource(null); setError(null); setLoading(false); onReady(null);
    return () => { generation.current++; onReady(null); };
  }, [identity, onReady]);
  async function preview() {
    const stamp = ++generation.current;
    setResource(null); setError(null); setLoading(true); onReady(null);
    try {
      const value = await previewShareDisclosure(resourceType, resourceId);
      if (stamp !== generation.current) return;
      if (value.resourceType !== resourceType || value.resourceId.toLowerCase() !== resourceId.toLowerCase() ||
          value.projectionVersion !== 'v1' || !(value.project || value.document))
        throw new Error('Nội dung xem trước không hợp lệ.');
      setResource(value); setLoadedIdentity(identity); onReady(identity);
    } catch (failure) {
      if (stamp === generation.current) setError(failure instanceof NexoraApiError && failure.status === 404
        ? 'Resource hoặc quyền chia sẻ không khả dụng. Document phải Published.'
        : failure instanceof NexoraApiError ? failure.message : 'Không thể tải nội dung xem trước. Hãy thử lại.');
    } finally { if (stamp === generation.current) setLoading(false); }
  }
  return <section aria-label="Nội dung sẽ được chia sẻ">
    <h3>Nội dung sẽ được chia sẻ</h3>
    <p className="field-help">Đây là projection read-only hiện tại. Nội dung nguồn có thể cập nhật sau khi tạo link. History, audit, reminder, private notes và Document con không được chia sẻ.</p>
    <button className="secondary-button" type="button" disabled={disabled || loading || !resourceId} onClick={() => void preview()}>{loading ? 'Đang kiểm tra…' : 'Xem trước nội dung chia sẻ'}</button>
    {loading && <p role="status">Server đang kiểm tra quyền và nội dung nguồn.</p>}
    {error && <p role="alert">{error}</p>}
    {resource && loadedIdentity === identity && render(resource)}
  </section>;
}
