import { useEffect, useRef, useState } from 'react';
import { getProject, listProjects, type ProjectRecord } from './api';

// Read only existing owner-authorized Project APIs; IDs stay in the command contract.
export function AutomationProjectPicker({ value, onChange, onAuthLost }: { value: string; onChange: (id: string) => void; onAuthLost: (e: unknown) => void }) {
  const generation = useRef(0), flight = useRef(false);
  const [items, setItems] = useState<ProjectRecord[]>([]), [selected, setSelected] = useState<ProjectRecord | null>(null);
  const [cursor, setCursor] = useState<string | null>(null), [back, setBack] = useState<string[]>([]);
  const [refresh, setRefresh] = useState(0);
  const [page, setPage] = useState(''), [busy, setBusy] = useState(false), [error, setError] = useState('');
  const active = (project: ProjectRecord) => !['Completed', 'Skipped', 'Deleted'].includes(project.status);
  useEffect(() => {
    const stamp = ++generation.current; flight.current = true; setBusy(true); setError(''); setItems([]); setSelected(null); setCursor(null);
    async function read() {
      try {
        const result = await listProjects(25, page);
        let current = value ? result.items.find(project => project.id === value) ?? null : null;
        if (value && !current) {
          try { current = await getProject(value); }
          catch (e) { if ((e as { status?: number }).status !== 404) throw e; }
        }
        if (stamp !== generation.current) return;
        setItems(result.items.filter(active)); setSelected(current && active(current) ? current : null); setCursor(result.nextCursor);
        if (value && (!current || !active(current))) setError('Project đã chọn không còn khả dụng. Chọn Project hiện tại để rebind.');
      } catch (e) {
        if (stamp !== generation.current) return;
        setItems([]); setSelected(null); setCursor(null);
        if ((e as { status?: number }).status === 401) onAuthLost(e);
        else setError(e instanceof Error ? e.message : 'Không tải được Projects.');
      } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
    }
    void read(); return () => { generation.current++; };
  }, [page, value, refresh]);
  const options = selected && !items.some(project => project.id === selected.id) ? [selected, ...items] : items;
  return <div><label>Project<select value={value} disabled={busy} required onChange={e => onChange(e.target.value)}>
    <option value="">Chọn Project</option>{value && !selected && !items.some(project => project.id === value) && <option value={value}>Project cần rebind hoặc xác minh</option>}
    {options.map(project => <option key={project.id} value={project.id}>{project.name} · {project.status}</option>)}
  </select></label>
  {busy && <p role="status">Đang tải Projects…</p>}{error && <p role="alert">{error}</p>}
  {!busy && !error && items.length === 0 && <p>Trang này không có Project nhận Task; chuyển trang hoặc tạo Project trước.</p>}
  <button type="button" disabled={busy || back.length === 0} onClick={() => { if (flight.current) return; const prior = back[back.length - 1]; setBack(old => old.slice(0, -1)); setPage(prior); }}>Projects trước</button>
  <button type="button" disabled={busy || !cursor} onClick={() => { if (flight.current || !cursor) return; setBack(old => [...old, page]); setPage(cursor); }}>25 Projects tiếp</button>
  {error && <button type="button" disabled={busy} onClick={() => { setBack([]); setPage(''); setRefresh(old => old + 1); onChange(''); }}>Tải lại và chọn Project</button>}
  {selected && <p>{selected.name}: {selected.startAt ? new Date(selected.startAt).toLocaleString() : 'Không giới hạn bắt đầu'} – {selected.endAt ? new Date(selected.endAt).toLocaleString() : 'Không giới hạn kết thúc'}. Server kiểm tra lại quyền và giới hạn khi Validate/Enable/Run.</p>}
  </div>;
}
