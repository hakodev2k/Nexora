import { useEffect, useRef, useState } from 'react';
import { getGoalProgressHistory, NexoraApiError, type GoalProgressPage } from './api';

export function GoalProgressHistory({ goalId, timeZoneId, locale, onAuthLost }: {
  goalId: string; timeZoneId: string; locale: string; onAuthLost: () => Promise<void>;
}) {
  const [page, setPage] = useState<GoalProgressPage | null>(null);
  const [loading, setLoading] = useState(false), [error, setError] = useState<string | null>(null);
  const generation = useRef(0), abort = useRef<AbortController | null>(null);
  useEffect(() => () => { ++generation.current; abort.current?.abort(); }, []);

  async function load(cursor: string | null = null) {
    const current = ++generation.current;
    abort.current?.abort();
    const controller = new AbortController(); abort.current = controller;
    setPage(null); setError(null); setLoading(true);
    try {
      const result = await getGoalProgressHistory(goalId, cursor, controller.signal);
      if (current === generation.current) setPage(result);
    } catch (failure) {
      if (current !== generation.current || controller.signal.aborted) return;
      setPage(null);
      const apiError = failure instanceof NexoraApiError ? failure : null;
      setError(apiError?.message ?? 'Không tải được lịch sử tiến độ.');
      if (apiError?.status === 401) await onAuthLost();
    } finally { if (current === generation.current) setLoading(false); }
  }

  function time(value: string) {
    try { return new Intl.DateTimeFormat(locale === 'en' ? 'en-US' : 'vi-VN', { dateStyle: 'medium', timeStyle: 'short', timeZone: timeZoneId }).format(new Date(value)); }
    catch { return 'Không xác định'; }
  }

  return <section className="form-panel" aria-labelledby="goal-progress-history-title">
    <div className="section-heading"><h3 id="goal-progress-history-title">Lịch sử tiến độ</h3>
      <button className="secondary-button" type="button" disabled={loading} onClick={() => void load()}>{loading ? 'Đang tải…' : page ? 'Tải lại từ đầu' : 'Xem lịch sử'}</button>
    </div>
    <p className="muted">Các lần ghi giá trị và ghi chú đã lưu; tên target là tên hiện tại. Đây chưa phải toàn bộ lịch sử chỉnh sửa Goal.</p>
    {loading && <p role="status">Đang tải lịch sử…</p>}
    {error && <p role="alert">{error}</p>}
    {page && (page.items.length === 0 ? <p>Chưa có lần ghi tiến độ nào.</p> : <>
      <div className="table-wrap"><table><caption className="sr-only">Lịch sử giá trị và ghi chú</caption><thead><tr><th scope="col">Thời điểm</th><th scope="col">Target hiện tại</th><th scope="col">Giá trị đã ghi</th><th scope="col">Ghi chú</th></tr></thead><tbody>
        {page.items.map(item => <tr key={item.id}><td><time dateTime={item.recordedAt}>{time(item.recordedAt)}</time></td><td>{item.currentTargetTitle}{item.targetRemoved && <span className="muted"> · Đã bỏ target</span>}</td><td>{item.kind === 'Boolean' ? item.checked ? 'Có' : 'Chưa' : item.valueExact}</td><td style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{item.note ?? '—'}</td></tr>)}
      </tbody></table></div>
      {page.nextCursor && <button className="secondary-button" type="button" disabled={loading} onClick={() => void load(page.nextCursor)}>Trang tiếp</button>}
    </>)}
  </section>;
}
