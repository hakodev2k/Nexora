import { useEffect, useRef, useState } from 'react';
import { NexoraApiError, openDashboardCreate, type DashboardCreateType } from './api';

const targets: { type: DashboardCreateType; module: string; title: string }[] = [
  { type: 'Project', module: 'FX11', title: 'Project' },
  { type: 'Task', module: 'FX12', title: 'Task' },
  { type: 'CalendarEvent', module: 'FX13', title: 'Calendar Event' },
  { type: 'Document', module: 'FX20', title: 'Document' },
  { type: 'FinanceRecord', module: 'FX27', title: 'Finance record' }
];

export function DashboardQuickCreate({ enabledModules, onOpen, onDenied, onAuthLost }: {
  enabledModules: string[]; onOpen: (moduleCode: string) => void;
  onDenied: () => void; onAuthLost: () => Promise<void>;
}) {
  const [busy, setBusy] = useState<DashboardCreateType | null>(null), [error, setError] = useState<string | null>(null);
  const mounted = useRef(false), flight = useRef(false), generation = useRef(0);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++generation.current; }; }, []);
  async function open(type: DashboardCreateType) {
    if (flight.current) return;
    flight.current = true; const stamp = ++generation.current; setBusy(type); setError(null);
    try {
      const target = await openDashboardCreate(type);
      if (mounted.current && stamp === generation.current) onOpen(target.moduleCode);
    } catch (value) {
      if (!mounted.current || stamp !== generation.current) return;
      const failure = value instanceof NexoraApiError ? value : new NexoraApiError('Không thể mở biểu mẫu. Hãy thử lại.', 0, 'NetworkError');
      setError(failure.message);
      if ([401, 403, 404, 409].includes(failure.status)) onDenied();
      if (failure.status === 401) await onAuthLost();
    } finally {
      flight.current = false;
      if (mounted.current && stamp === generation.current) setBusy(null);
    }
  }
  const available = targets.filter(target => enabledModules.includes(target.module));
  if (available.length === 0) return null;
  return <section className="form-panel" aria-labelledby="dashboard-create-title">
    <h2 id="dashboard-create-title">Tạo mới</h2>
    <p>Bắt đầu một việc mới. Bạn có thể xem lại thông tin trước khi lưu.</p>
    <div className="form-actions">{available.map(target => <button key={target.type} type="button" className="secondary-button"
      disabled={busy !== null} onClick={() => void open(target.type)}>{busy === target.type ? 'Đang kiểm tra quyền…' : `Tạo ${target.title}`}</button>)}</div>
    {error && <p role="alert">{error}</p>}
  </section>;
}
