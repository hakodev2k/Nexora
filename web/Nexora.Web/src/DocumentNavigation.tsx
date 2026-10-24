import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey, listDocuments, type DocumentSummary } from './api';
import { documentCapabilities, documentFolders, documentTrash, getDocumentFolder, writeDocumentFolder, type Folder, type TrashEntry, type TreeTarget } from './documentTopologyApi';
import { ResourceFormDialog } from './ResourceFormDialog';

export function DocumentNavigation({ folder, suspended, disabled, onFolder, onTree, beforeEdit, onCommitted, onUnavailable, onAuthLost }: { folder: Folder | null; suspended: boolean; disabled: boolean; onFolder: (value: Folder | null) => void; onTree: (target: TreeTarget) => void; beforeEdit: (proceed: () => void) => void; onCommitted: () => void; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [folders, setFolders] = useState<Folder[]>([]), [trash, setTrash] = useState<TrashEntry[]>([]), [mode, setMode] = useState<'folders' | 'trash'>('folders'), [next, setNext] = useState<string | null>(null), [error, setError] = useState(''), [busy, setBusy] = useState(false), [caps, setCaps] = useState<Record<string, boolean>>({}), [reload, setReload] = useState(0);
  const [editor, setEditor] = useState<{ folder: Folder | null; parent: string | null } | null>(null);
  const generation = useRef(0), callbacks = useRef({ onUnavailable, onAuthLost }); callbacks.current = { onUnavailable, onAuthLost };
  async function load(cursor = '') {
    if (suspended || disabled) return; const epoch = ++generation.current; setBusy(true); setFolders([]); setTrash([]); setNext(null); setError(''); setCaps({});
    try {
      const [permissions, result] = await Promise.all([documentCapabilities(), mode === 'folders' ? documentFolders(folder?.id, cursor) : documentTrash(cursor)]);
      if (epoch !== generation.current) return;
      setCaps(permissions); setNext(result.nextCursor);
      if (mode === 'folders') setFolders(result.items as Folder[]); else setTrash(result.items as TrashEntry[]);
    } catch (value) {
      if (epoch !== generation.current) return; const failure = value as { message?: string; status?: number }; setError(failure.message ?? 'Không thể tải thư mục.');
      if ([401, 403, 404].includes(failure.status ?? 0)) { callbacks.current.onUnavailable(); if (failure.status === 401) void callbacks.current.onAuthLost(); }
    } finally { if (epoch === generation.current) setBusy(false); }
  }
  useEffect(() => {
    generation.current++; setFolders([]); setTrash([]); setNext(null); setCaps({});
    if (!suspended && !disabled) void load();
    return () => { generation.current++; };
    // Parent navigation and explicit reload delimit the protected read context.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [folder?.id, mode, suspended, disabled, reload]);
  const blocked = disabled || busy || suspended;
  async function back() {
    if (!folder?.parentFolderId || blocked) return; const epoch = ++generation.current; setBusy(true); setError('');
    try { const parent = await getDocumentFolder(folder.parentFolderId); if (epoch === generation.current) onFolder(parent); }
    catch (value) { if (epoch === generation.current) { setFolders([]); setTrash([]); setCaps({}); setError(value instanceof Error ? value.message : 'Không thể đọc thư mục cha.'); callbacks.current.onUnavailable(); if ((value as { status?: number }).status === 401) void callbacks.current.onAuthLost(); } }
    finally { if (epoch === generation.current) setBusy(false); }
  }
  return <aside className="form-panel" aria-label="Document navigation"><h2>Thư mục</h2>
    <div className="form-actions"><button type="button" disabled={disabled || busy} onClick={() => onFolder(null)}>Library gốc</button><button type="button" disabled={blocked} onClick={() => setMode(value => value === 'folders' ? 'trash' : 'folders')}>{mode === 'folders' ? 'Thùng rác' : 'Thư mục'}</button><button type="button" disabled={blocked} onClick={() => setReload(value => value + 1)}>Tải lại thư mục</button></div>
    {folder && !suspended && <nav aria-label="Đường dẫn thư mục"><span>Library / </span>{folder.parentFolderId && <button type="button" disabled={blocked} onClick={() => void back()}>Thư mục cha / </button>}<span aria-current="location">{folder.title}</span></nav>}{error && <p role="alert">{error}</p>}{busy && <p role="status">Đang tải…</p>}
    {mode === 'folders' && <><div className="form-actions">{caps['documents.folder.create'] && (!folder || folder.depth < 2) && <button type="button" disabled={blocked} onClick={() => beforeEdit(() => setEditor({ folder: null, parent: folder?.id ?? null }))}>Tạo thư mục</button>}{folder && caps['documents.folder.rename'] && <button type="button" disabled={blocked} onClick={() => beforeEdit(() => setEditor({ folder, parent: folder.parentFolderId }))}>Đổi tên</button>}{folder && caps['documents.folder.trash'] && <button type="button" disabled={blocked} onClick={() => onTree({ id: folder.id, kind: 'Folder', operation: 'trash' })}>Đưa thư mục vào Thùng rác</button>}</div><ul>{folders.map(item => <li key={item.id}><button type="button" disabled={blocked} onClick={() => onFolder(item)}>{item.title}</button></li>)}</ul></>}
    {mode === 'trash' && <ul>{trash.map(item => <li key={item.kind + item.id}>{item.title ?? 'Mục không có quyền đọc'} · {item.kind} · {item.deletedAt}{(['restore', 'purge'] as const).map(operation => caps[`documents.${item.kind.toLowerCase()}.${operation}`] && <button key={operation} type="button" disabled={blocked} onClick={() => onTree({ id: item.id, kind: item.kind, operation })}>{operation === 'restore' ? 'Khôi phục' : 'Xóa vĩnh viễn'}</button>)}</li>)}</ul>}
    {next && <button type="button" disabled={blocked} onClick={() => void load(next)}>Trang tiếp</button>}
    {editor && <FolderEditor editor={editor} onClose={() => setEditor(null)} onCommitted={() => { setEditor(null); setFolders([]); setTrash([]); setCaps({}); onCommitted(); }} onUnavailable={onUnavailable} onAuthLost={onAuthLost} />}
  </aside>;
}

function FolderEditor({ editor, onClose, onCommitted, onUnavailable, onAuthLost }: { editor: { folder: Folder | null; parent: string | null }; onClose: () => void; onCommitted: () => void; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [title, setTitle] = useState(editor.folder?.title ?? ''), [busy, setBusy] = useState(false), [error, setError] = useState(''), [intent, setIntent] = useState<{ title: string; key: string } | null>(null); const flight = useRef(false);
  async function save() {
    if (flight.current || (!intent && !title.trim())) return; const command = intent ?? { title: title.trim(), key: createIdempotencyKey() }; flight.current = true; setBusy(true); setIntent(command); setError('');
    try { await writeDocumentFolder(editor.folder, command.title, editor.parent, command.key); setIntent(null); onCommitted(); }
    catch (value) {
      const failure = value as { status?: number; code?: string; message?: string }; setError(failure.message ?? 'Kết quả chưa rõ.');
      if (failure.status && failure.status < 500 && !['RequestInProgress', 'IdempotencyConflict'].includes(failure.code ?? '')) setIntent(null);
      if ([401, 403, 404].includes(failure.status ?? 0)) { setTitle(''); onUnavailable(); if (failure.status === 401) void onAuthLost(); }
    } finally { flight.current = false; setBusy(false); }
  }
  return <ResourceFormDialog open title={editor.folder ? 'Đổi tên thư mục' : 'Tạo thư mục'} busy={busy} dirty={intent !== null || title !== (editor.folder?.title ?? '')} onClose={onClose}><form onSubmit={event => { event.preventDefault(); void save(); }}><label>Tên thư mục<input maxLength={200} value={title} disabled={busy || intent !== null} onChange={event => setTitle(event.target.value)} /></label>{error && <p role="alert">{error}</p>}{intent && <p>Bỏ yêu cầu không hoàn tác thao tác đã gửi.</p>}<button type="submit" disabled={busy || (!intent && !title.trim())}>{intent ? 'Đối chiếu cùng yêu cầu' : 'Lưu'}</button></form></ResourceFormDialog>;
}

export function DocumentChildren({ parentId, suspended, disabled, onOpen, onUnavailable }: { parentId: string; suspended: boolean; disabled: boolean; onOpen: (item: DocumentSummary) => void; onUnavailable: (status: number) => void }) {
  const [items, setItems] = useState<DocumentSummary[]>([]), [next, setNext] = useState<string | null>(null), [busy, setBusy] = useState(false), [error, setError] = useState(''); const generation = useRef(0);
  const callbacks = useRef({ onUnavailable }); callbacks.current = { onUnavailable };
  async function load(cursor = '') {
    if (suspended || disabled) return; const epoch = ++generation.current; setItems([]); setNext(null); setBusy(true); setError('');
    try { const page = await listDocuments(undefined, 25, cursor, '', '', undefined, '', '', '', undefined, parentId); if (epoch === generation.current) { setItems(page.items); setNext(page.nextCursor); } }
    catch (value) { if (epoch === generation.current) { setError(value instanceof Error ? value.message : 'Không thể tải trang con.'); const status = (value as { status?: number }).status ?? 0; if ([401, 403, 404].includes(status)) callbacks.current.onUnavailable(status); } }
    finally { if (epoch === generation.current) setBusy(false); }
  }
  useEffect(() => { generation.current++; setItems([]); setNext(null); if (!suspended && !disabled) void load(); return () => { generation.current++; }; }, [parentId, suspended, disabled]);
  return <aside className="form-panel"><h2>Trang con</h2>{error && <p role="alert">{error}</p>}{busy && <p role="status">Đang tải…</p>}<ul>{items.map(item => <li key={item.id}><button type="button" disabled={disabled || suspended || busy} onClick={() => onOpen(item)}>{item.title}</button> · {item.status}</li>)}</ul>{next && <button type="button" disabled={disabled || suspended || busy} onClick={() => void load(next)}>Trang tiếp</button>}</aside>;
}
