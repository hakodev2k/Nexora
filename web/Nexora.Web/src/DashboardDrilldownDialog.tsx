import { useEffect, useRef, useState } from 'react';
import { getDashboardDrilldown, type DashboardDrilldownPage } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';

function instantLabel(value: string, timeZone: string) {
  try { return new Intl.DateTimeFormat('vi', { timeZone, dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)); }
  catch { return value; }
}

export function dashboardScopeLabel(key: string) {
  return ({
    'tasks-due': 'Task chưa hoàn tất trong Project đang hoạt động, có hạn trước cuối ngày tham chiếu.',
    'calendar-today': 'Event Scheduled/Completed giao với ngày tham chiếu; Task projection theo quyền đọc hiện tại.',
    'documents-recent': 'Document Draft/Published còn khả dụng, mới cập nhật trước; không giới hạn ngày.',
    'notifications-unread': 'Tất cả thông báo chưa đọc trong Inbox hiện tại, mới nhất trước.'
  } as Record<string, string>)[key] ?? 'Phạm vi nguồn hiện tại.';
}

export function DashboardDrilldownDialog({ token, onClose, onUnavailable, onAuthLost, onOpenSource }: {
  token: string; onClose: () => void; onUnavailable: () => void; onAuthLost: () => Promise<void>;
  onOpenSource: (type: 'Task' | 'Event' | 'Document', id: string) => void;
}) {
  const [currentToken, setCurrentToken] = useState(token), [page, setPage] = useState<DashboardDrilldownPage | null>(null);
  const [loading, setLoading] = useState(true), [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0), [expired, setExpired] = useState(false);
  const callbacks = useRef({ onUnavailable, onAuthLost }); callbacks.current = { onUnavailable, onAuthLost };
  useEffect(() => {
    let cancelled = false; const controller = new AbortController();
    setPage(null); setLoading(true); setError(null); setExpired(false);
    getDashboardDrilldown(currentToken, controller.signal).then(value => {
      if (cancelled) return;
      if (!value || !value.widget || !['Ready', 'Empty'].includes(value.widget.state) || !Array.isArray(value.widget.items)
        || value.widget.items.length > 25 || !Number.isSafeInteger(value.widget.count) || value.widget.count < 0)
        throw new Error('Dữ liệu widget không hợp lệ. Hãy tải lại Dashboard.');
      setPage(value);
    }).catch(value => {
      if (cancelled) return;
      setPage(null); setError(value instanceof Error ? value.message : 'Widget không khả dụng.');
      const status = (value as { status?: number }).status;
      callbacks.current.onUnavailable();
      if ([401, 403, 404, 409, 410].includes(status ?? 0)) setExpired(true);
      if (status === 401) void callbacks.current.onAuthLost();
    }).finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; controller.abort(); };
  }, [currentToken, retry]);
  function move(next: string | null) { if (!next || loading) return; setPage(null); setLoading(true); setCurrentToken(next); }
  return <ResourceFormDialog open title="Dashboard · Chi tiết widget" busy={false} dirty={false} onClose={onClose}>
    {loading && <p role="status">Đang kiểm tra nguồn hiện tại…</p>}
    {error && <p role="alert">{error}</p>}
    {expired ? <p>Đóng cửa sổ và tải lại Dashboard để lấy phạm vi hiện tại.</p> : error && <button type="button" disabled={loading} onClick={() => { setPage(null); setRetry(value => value + 1); }}>Thử lại</button>}
    {page && <section aria-label={page.widget.title}>
      <h3>{page.widget.title}</h3>
      <p>{page.widget.count} mục hiện tại · {page.timeZoneId}</p>
      <p>{dashboardScopeLabel(page.widget.id)}</p>
      <p>Ngày tham chiếu: {page.localDate} · phạm vi lấy lúc {page.capturedAt}.</p>
      <p>Dữ liệu nguồn được đọc lại lúc {page.widget.refreshedAt}; thay đổi nguồn có thể làm dịch chuyển các trang.</p>
      {page.widget.message && <p>{page.widget.message}</p>}
      {page.widget.items.length === 0 ? <p>Trang này không có mục hiện tại.</p> : <ul className="grant-list">{page.widget.items.map(item => <li key={item.id}>
        <span><strong>{item.title}</strong><small>{item.status ?? item.kind}
          {item.startDate && item.endDateExclusive ? ` · ${item.startDate} — ${item.endDateExclusive} (end exclusive)` : ''}
          {item.at ? ` · ${instantLabel(item.at, page.timeZoneId)}` : ''}</small></span>
        {page.widget.sourceModule === 'FX12' || page.widget.sourceModule === 'FX13' || page.widget.sourceModule === 'FX20'
          ? <button type="button" className="secondary-button" onClick={() => onOpenSource(page.widget.sourceModule === 'FX12' ? 'Task' : page.widget.sourceModule === 'FX13' ? 'Event' : 'Document', item.id)}>Mở nguồn hiện tại</button> : null}
      </li>)}</ul>}
      <div className="form-actions"><button type="button" disabled={loading || !page.previousToken} onClick={() => move(page.previousToken)}>Trang trước</button>
        <button type="button" disabled={loading || !page.nextToken} onClick={() => move(page.nextToken)}>Trang sau</button></div>
    </section>}
  </ResourceFormDialog>;
}
