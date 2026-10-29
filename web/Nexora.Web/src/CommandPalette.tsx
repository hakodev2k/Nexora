import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react';
import { NexoraApiError, searchResources } from './api';
import { executeCommand, listCommands, type CommandHandoff, type CommandSelection } from './commandApi';
import { recentList, recentTypes, type RecentType } from './recentApi';
import { ResourceFormDialog } from './ResourceFormDialog';

type Option = { id: string; title: string; group: string; selection: CommandSelection };
export function CommandPalette({ onClose, onHandoff, onAuthLost }: {
  onClose: () => void; onHandoff: (value: CommandHandoff) => void; onAuthLost: () => Promise<void>;
}) {
  const [query, setQuery] = useState(''), [options, setOptions] = useState<Option[]>([]), [active, setActive] = useState(-1);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [canExecute, setCanExecute] = useState(false);
  const [error, setError] = useState<string | null>(null), [sourceHint, setSourceHint] = useState<string | null>(null), [retry, setRetry] = useState(0), [composing, setComposing] = useState(false);
  const epoch = useRef(0), mounted = useRef(false), flight = useRef(false);
  const callbacks = useRef({ onHandoff, onAuthLost }); callbacks.current = { onHandoff, onAuthLost };
  const listId = useId();
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++epoch.current; }; }, []);
  useEffect(() => {
    const stamp = ++epoch.current, controller = new AbortController();
    setOptions([]); setActive(-1); setCanExecute(false); setError(null); setSourceHint(null); setLoading(!composing);
    if (composing) return () => controller.abort();
    const timer = window.setTimeout(() => { void (async () => {
      try {
        const page = await listCommands(query.trim(), controller.signal);
        if (!mounted.current || stamp !== epoch.current) return;
        setCanExecute(page.canExecute);
        const commands: Option[] = page.items.map(item => ({ id: 'command:' + item.key, title: item.title, group: item.group, selection: { kind: 'Command', key: item.key } }));
        setOptions(commands);
        try {
          let sources: Option[] = [];
          if (query.trim() && page.canSearch) {
            const result = await searchResources(query.trim(), '', '', '', false, 25, controller.signal);
            if (!mounted.current || stamp !== epoch.current) return;
            if (result.providers.some(provider => provider.state === 'Unavailable' || provider.state === 'Degraded')) setSourceHint('Một số nguồn không khả dụng; kết quả là một phần dữ liệu hiện tại.');
            sources = result.items.filter(item => recentTypes.includes(item.resourceType as RecentType)).map(item => ({ id: item.resourceType + ':' + item.id, title: item.title, group: 'Nguồn tìm thấy', selection: { kind: 'Resource', resourceType: item.resourceType as RecentType, resourceId: item.id } }));
          } else if (!query.trim() && page.canReadRecent) {
            const result = await recentList('', undefined, controller.signal);
            if (!mounted.current || stamp !== epoch.current) return;
            sources = result.items.map(item => ({ id: item.resourceType + ':' + item.resourceId, title: item.title, group: 'Đã mở gần đây', selection: { kind: 'Resource', resourceType: item.resourceType, resourceId: item.resourceId } }));
          }
          if (mounted.current && stamp === epoch.current) setOptions([...commands, ...sources]);
        } catch (value) {
          if (!mounted.current || stamp !== epoch.current || controller.signal.aborted) return;
          setSourceHint('Không thể đọc nguồn hiện tại. Các lệnh khả dụng vẫn được giữ; thử lại để kiểm tra nguồn.');
          if ((value as { status?: number }).status === 401) { setOptions([]); setCanExecute(false); void callbacks.current.onAuthLost(); }
        }
      } catch (value) {
        if (!mounted.current || stamp !== epoch.current || controller.signal.aborted) return;
        setOptions([]); setCanExecute(false); setError(value instanceof Error ? value.message : 'Command Palette không khả dụng.');
        if ((value as { status?: number }).status === 401) void callbacks.current.onAuthLost();
      } finally { if (mounted.current && stamp === epoch.current) setLoading(false); }
    })(); }, query.trim() ? 250 : 0);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [query, retry, composing]);
  useEffect(() => { if (active >= 0) document.getElementById(listId + '-' + active)?.scrollIntoView?.({ block: 'nearest' }); }, [active, listId]);
  async function choose(option: Option) {
    if (flight.current || loading || !canExecute) return;
    flight.current = true; const stamp = ++epoch.current; setBusy(true); setError(null);
    try {
      const handoff = await executeCommand(option.selection);
      if (mounted.current && stamp === epoch.current) callbacks.current.onHandoff(handoff);
    } catch (value) {
      if (!mounted.current || stamp !== epoch.current) return;
      setOptions([]); setActive(-1); setCanExecute(false); setSourceHint(null);
      setError(value instanceof Error ? value.message : 'Không thể mở luồng hiện tại.');
      if (value instanceof NexoraApiError && value.status === 401) void callbacks.current.onAuthLost();
    } finally { flight.current = false; if (mounted.current && stamp === epoch.current) setBusy(false); }
  }
  function keyboard(event: KeyboardEvent<HTMLInputElement>) {
    if (event.nativeEvent.isComposing || composing || loading || busy) return;
    if ((event.key === 'ArrowDown' || event.key === 'ArrowUp') && options.length) {
      event.preventDefault(); setActive(current => event.key === 'ArrowDown' ? (current + 1) % options.length : current <= 0 ? options.length - 1 : current - 1);
    } else if (event.key === 'Enter' && active >= 0 && options[active]) { event.preventDefault(); void choose(options[active]); }
  }
  const groups = [...new Set(options.map(option => option.group))];
  return <ResourceFormDialog open title="Command Palette" busy={busy} dirty={false} onClose={onClose}>
    <label htmlFor={listId + '-query'}>Tìm lệnh hoặc nguồn</label>
    <input id={listId + '-query'} role="combobox" aria-autocomplete="list" aria-expanded="true" aria-controls={listId}
      aria-activedescendant={active >= 0 && options[active] ? listId + '-' + active : undefined} autoComplete="off" maxLength={500}
      value={query} disabled={busy} onChange={event => { ++epoch.current; setOptions([]); setActive(-1); setLoading(true); setQuery(event.target.value); }}
      onCompositionStart={() => setComposing(true)} onCompositionEnd={() => setComposing(false)} onKeyDown={keyboard}
      onKeyDownCapture={event => { if ((event.nativeEvent.isComposing || composing) && event.key === 'Escape') event.stopPropagation(); }} />
    <p>↑/↓ chọn mục · Enter mở · Escape đóng. Chỉ tô sáng không thực hiện thao tác.</p>
    {loading && <p role="status">Đang kiểm tra lệnh và nguồn hiện tại…</p>}
    {error && <p role="alert">{error}</p>}{sourceHint && <p role="status">{sourceHint}</p>}
    {(!canExecute && !loading && !error) && <p>Không có quyền thực thi lệnh hiện tại.</p>}
    {!loading && !error && options.length === 0 && <p>Không có mục khả dụng phù hợp.</p>}
    <div id={listId} role="listbox" aria-label="Lệnh và nguồn khả dụng">{groups.map(group => <div role="group" aria-label={group} key={group}>
      <p role="presentation">{group}</p>{options.map((option, index) => option.group !== group ? null : <div role="option" tabIndex={-1} key={option.id} id={listId + '-' + index}
        aria-selected={active === index} aria-disabled={!canExecute || busy || loading} className="module-card palette-option"
        onMouseDown={event => event.preventDefault()} onClick={() => { setActive(index); void choose(option); }}
        onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void choose(option); } }}>
        <strong>{option.title}</strong>{option.selection.kind === 'Resource' && <small> · {option.selection.resourceType}</small>}
      </div>)}
    </div>)}</div>
    <button type="button" disabled={busy || loading} onClick={() => { setOptions([]); setRetry(value => value + 1); }}>Kiểm tra lại</button>
  </ResourceFormDialog>;
}
