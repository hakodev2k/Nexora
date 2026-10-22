import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey, getDashboardLayout, isProductivityAcknowledgement, NexoraApiError, saveDashboardLayout,
  type DashboardLayout, type DashboardWidgetConfig } from './api';
import { registerDirtyLeaveGuard } from './navigationGuard';
import { ResourceFormDialog } from './ResourceFormDialog';

const providers = [
  { key: 'tasks-due', title: 'Tasks due and overdue' }, { key: 'calendar-today', title: 'Calendar today' },
  { key: 'documents-recent', title: 'Recent Documents' }, { key: 'notifications-unread', title: 'Unread Notifications' }
];
type DraftWidget = DashboardWidgetConfig & { localId: string };
function draft(widget: DashboardWidgetConfig): DraftWidget { return { ...widget, localId: widget.id ?? createIdempotencyKey() }; }
function newWidget(key: string): DraftWidget { return draft({ id: null, key, providerVersion: '1', configVersion: 1, widthUnits: 1, enabled: true, options: {} }); }

export function DashboardLayoutEditor({ onLayout, onCommit, onUnavailable, onAuthLost, refreshToken }: {
  onLayout: (layout: DashboardLayout | null) => void; onCommit: (readable: boolean) => void; onUnavailable: () => void; onAuthLost: () => Promise<void>; refreshToken: number;
}) {
  const [current, setCurrent] = useState<DashboardLayout | null>(null), [widgets, setWidgets] = useState<DraftWidget[]>([]);
  const [loaded, setLoaded] = useState(false), [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false);
  const [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const epoch = useRef(0), mounted = useRef(false), flight = useRef(false);
  const callbacks = useRef({ onLayout, onCommit, onUnavailable, onAuthLost }); callbacks.current = { onLayout, onCommit, onUnavailable, onAuthLost };
  const pending = useRef<(() => ReturnType<typeof saveDashboardLayout>) | null>(null);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const baseline = useRef('');
  const serialized = JSON.stringify(widgets.map(({ localId: _localId, ...widget }) => widget));
  const dirty = uncertain || (loaded && serialized !== baseline.current);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !pending.current) return false; setLeave({ proceed }); return true; }, 1), [dirty, busy]);
  useEffect(() => {
    if (!dirty && !pending.current) return;
    const prevent = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent);
  }, [dirty, busy]);
  function failure(value: unknown) {
    const apiError = value instanceof NexoraApiError ? value : new NexoraApiError('Không thể đọc hoặc lưu bố cục.', 0, 'NetworkError');
    setError(apiError.message);
    if ([401, 403, 404].includes(apiError.status) || ['ModuleUnavailable', 'ActionDenied', 'DependencyUnavailable'].includes(apiError.code ?? '')) {
      ++epoch.current; pending.current = null; setCurrent(null); setWidgets([]); setLoaded(false); setUncertain(false); setBusy(false); callbacks.current.onUnavailable();
    }
    if (apiError.status === 401) void callbacks.current.onAuthLost(); return apiError;
  }
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++epoch.current; pending.current = null; }; }, []);
  useEffect(() => {
    if (pending.current) return;
    const stamp = ++epoch.current, controller = new AbortController(); setBusy(true); setError(null); setLoaded(false);
    getDashboardLayout(controller.signal).then(result => {
      if (!mounted.current || stamp !== epoch.current) return;
      if (!result || !Object.prototype.hasOwnProperty.call(result, 'layout') || result.layout === undefined)
        throw new NexoraApiError('Phản hồi bố cục không hợp lệ. Hãy tải lại Dashboard.', 502, 'InvalidResponse');
      const next = result.layout ? result.layout.widgets.map(draft) : providers.map(provider => newWidget(provider.key));
      baseline.current = JSON.stringify(next.map(({ localId: _localId, ...widget }) => widget));
      setCurrent(result.layout); setWidgets(next); setLoaded(true); callbacks.current.onLayout(result.layout);
    }).catch(value => { if (mounted.current && stamp === epoch.current) { failure(value); callbacks.current.onUnavailable(); } })
      .finally(() => { if (mounted.current && stamp === epoch.current) setBusy(false); });
    return () => { controller.abort(); ++epoch.current; };
  }, [refreshToken]);
  async function execute() {
    const intent = pending.current; if (!intent || flight.current) return;
    flight.current = true; ++epoch.current; setBusy(true); setError(null); setNotice(null);
    try {
      const result = await intent(); if (!mounted.current || pending.current !== intent) return;
      pending.current = null; setUncertain(false);
      if (isProductivityAcknowledgement(result)) { setCurrent(null); setWidgets([]); setLoaded(false); callbacks.current.onCommit(false); }
      else { baseline.current = JSON.stringify(result.widgets); setCurrent(result); setWidgets(result.widgets.map(draft)); callbacks.current.onLayout(result); callbacks.current.onCommit(true); }
      setNotice('Bố cục đã được lưu; source payload vẫn được kiểm tra quyền riêng.');
    } catch (value) {
      if (!mounted.current || pending.current !== intent) return; const apiError = failure(value);
      const unknown = apiError.status === 0 || apiError.status >= 500 || apiError.status === 408 || ['RequestInProgress', 'IdempotencyConflict'].includes(apiError.code ?? '');
      if (unknown && pending.current === intent) setUncertain(true); else pending.current = null;
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function save() {
    if (pending.current || busy || !loaded) return;
    const key = createIdempotencyKey(), original = current;
    const snapshot = widgets.map(({ localId: _localId, ...widget }) => ({ ...widget, options: { ...widget.options } }));
    pending.current = () => saveDashboardLayout(original, snapshot, key); void execute();
  }
  function move(index: number, offset: number) {
    const next = index + offset; if (next < 0 || next >= widgets.length) return;
    setWidgets(previous => { const copy = [...previous]; [copy[index], copy[next]] = [copy[next], copy[index]]; return copy; });
  }
  const blocked = busy || uncertain || !loaded;
  return <section className="form-panel" aria-label="Bố cục Dashboard"><div className="section-heading"><h2>Bố cục Dashboard</h2><span>{current ? `Revision ${current.layoutVersion}` : 'Chưa có bố cục đã lưu'}</span></div>
    {error && <div className="notice notice-error" role="alert">{error}</div>}{notice && <p role="status">{notice}</p>}{busy && <p role="status">Đang xử lý bố cục…</p>}
    {uncertain && <div role="alert">Kết quả chưa rõ; các thay đổi đang được giữ nguyên.<button type="button" disabled={busy} onClick={() => void execute()}>Thử lại thao tác cũ</button></div>}
    <fieldset disabled={blocked}><legend>Widget đã đăng ký</legend><ol className="resource-cards">{widgets.map((widget, index) => <li className="resource-card" key={widget.localId}>
      <label>Nguồn<select value={widget.key} onChange={event => setWidgets(old => old.map(row => row.localId === widget.localId ? { ...row, key: event.target.value } : row))}>{!providers.some(provider => provider.key === widget.key) && <option value={widget.key}>Provider không tương thích</option>}{providers.map(provider => <option key={provider.key} value={provider.key}>{provider.title}</option>)}</select></label>
      <label>Kích thước<select value={widget.widthUnits} onChange={event => setWidgets(old => old.map(row => row.localId === widget.localId ? { ...row, widthUnits: Number(event.target.value) } : row))}><option value={1}>Một cột</option><option value={2}>Hai cột</option></select></label>
      <label className="check-row"><input type="checkbox" checked={widget.enabled} onChange={event => setWidgets(old => old.map(row => row.localId === widget.localId ? { ...row, enabled: event.target.checked } : row))} />Hiển thị</label>
      <div className="resource-actions"><button type="button" disabled={index === 0} aria-label={`Đưa widget ${index + 1} lên`} onClick={() => move(index, -1)}>↑</button><button type="button" disabled={index === widgets.length - 1} aria-label={`Đưa widget ${index + 1} xuống`} onClick={() => move(index, 1)}>↓</button><button type="button" onClick={() => setWidgets(old => old.filter(row => row.localId !== widget.localId))}>Gỡ widget</button></div>
    </li>)}</ol><button type="button" disabled={widgets.length >= 24} onClick={() => setWidgets(old => [...old, newWidget(providers[0].key)])}>Thêm widget</button><button type="button" className="primary-button" onClick={save}>Lưu bố cục</button></fieldset>
    <ResourceFormDialog open={leave !== null} title="Rời bố cục Dashboard" dirty={false} busy={busy} onClose={() => setLeave(null)}><p>Rời màn hình sẽ mất thay đổi/mã đối chiếu; request đã gửi có thể đã commit.</p><button disabled={busy} onClick={() => { const proceed = leave?.proceed; pending.current = null; ++epoch.current; setLeave(null); proceed?.(); }}>Bỏ thay đổi/mã đối chiếu và rời màn hình</button></ResourceFormDialog>
  </section>;
}
