import { Component, lazy, Suspense, useState, type ReactNode } from 'react';

const DocumentMarkdownPreview = lazy(() => import('./DocumentMarkdownPreview'));
class DocumentPreviewBoundary extends Component<{ body: string; children: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  componentDidUpdate(previous: Readonly<{ body: string; children: ReactNode }>) { if (this.state.failed && previous.body !== this.props.body) this.setState({ failed: false }); }
  render() { return this.state.failed ? <div><p role="alert">Bản xem trước không khả dụng. Nội dung gốc được giữ nguyên.</p><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{this.props.body}</pre></div> : this.props.children; }
}
function MarkdownView({ body }: { body: string }) {
  return <DocumentPreviewBoundary body={body}><Suspense fallback={<p role="status">Đang tải bản xem trước…</p>}><DocumentMarkdownPreview body={body} /></Suspense></DocumentPreviewBoundary>;
}

type TextBlock = { id: string; type: 'paragraph' | 'quote'; text: string };
type HeadingBlock = { id: string; type: 'heading'; text: string; level: number };
type CodeBlock = { id: string; type: 'code'; text: string; language?: string };
type LinkBlock = { id: string; type: 'link'; text: string; url: string };
type ListBlock = { id: string; type: 'list'; ordered: boolean; items: string[] };
type CheckBlock = { id: string; type: 'checklist'; items: { text: string; checked: boolean }[] };
type TableBlock = { id: string; type: 'table'; rows: string[][] };
type Block = TextBlock | HeadingBlock | CodeBlock | LinkBlock | ListBlock | CheckBlock | TableBlock | { id: string; type: 'divider' };
type BlockKind = Block['type'];
const kinds: BlockKind[] = ['paragraph', 'heading', 'list', 'checklist', 'quote', 'divider', 'link', 'code', 'table'];
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const isRecord = (value: unknown): value is Record<string, unknown> => !!value && typeof value === 'object' && !Array.isArray(value);
const fields = (value: Record<string, unknown>, allowed: string[]) => Object.keys(value).every(key => allowed.includes(key));
const text = (value: unknown): value is string => typeof value === 'string' && !value.includes('\0');
function safeUrl(value: unknown) { if (!text(value) || value.length > 2048 || /[\u0000-\u001f\u007f]/.test(value)) return false; try { const url = new URL(value); return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password && Boolean(url.hostname); } catch { return false; } }
// JSON.parse discards duplicate object keys. Detect them before allowing edits
// so loading a legacy payload cannot silently erase its original fields.
function uniqueKeys(body: string) {
  const stack: ({ keys: Set<string>; key: boolean } | null)[] = [];
  const tokens = body.matchAll(/"(?:\\.|[^"\\])*"|[{}\[\],]/g);
  for (const token of tokens) {
    const value = token[0];
    if (value === '{' || value === '[') { stack.push(value === '{' ? { keys: new Set(), key: true } : null); if (stack.length > 12) return false; }
    else if (value === '}' || value === ']') stack.pop();
    else { const object = stack[stack.length - 1]; if (value === ',') { if (object) object.key = true; }
      else if (object?.key) { const key = JSON.parse(value) as string; if (object.keys.has(key)) return false; object.keys.add(key); object.key = false; } }
  }
  return true;
}
function parse(body: string): Block[] | null {
  if (!body) return [];
  try {
    const root: unknown = JSON.parse(body);
    if (!uniqueKeys(body)) return null;
    if (!isRecord(root) || !fields(root, ['schemaVersion', 'blocks']) || root.schemaVersion !== 1 || !Array.isArray(root.blocks) || root.blocks.length > 1000) return null;
    const ids = new Set<string>();
    for (const block of root.blocks) {
      if (!isRecord(block) || !text(block.id) || !uuid.test(block.id) || /^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(block.id) || ids.has(block.id.toLowerCase()) || !kinds.includes(block.type as BlockKind)) return null;
      ids.add(block.id.toLowerCase());
      const base = ['id', 'type']; let valid = false;
      switch (block.type) {
        case 'paragraph': case 'quote': valid = fields(block, [...base, 'text']) && text(block.text); break;
        case 'heading': valid = fields(block, [...base, 'text', 'level']) && text(block.text) && [1, 2, 3].includes(block.level as number); break;
        case 'divider': valid = fields(block, base); break;
        case 'code': valid = fields(block, [...base, 'text', 'language']) && text(block.text) && (block.language === undefined || (text(block.language) && block.language.length <= 64)); break;
        case 'link': valid = fields(block, [...base, 'text', 'url']) && text(block.text) && text(block.url); break;
        case 'list': valid = fields(block, [...base, 'ordered', 'items']) && typeof block.ordered === 'boolean' && Array.isArray(block.items) && block.items.length <= 1000 && block.items.every(text); break;
        case 'checklist': valid = fields(block, [...base, 'items']) && Array.isArray(block.items) && block.items.length <= 1000 && block.items.every(item => isRecord(item) && fields(item, ['text', 'checked']) && text(item.text) && typeof item.checked === 'boolean'); break;
        case 'table': valid = fields(block, [...base, 'rows']) && Array.isArray(block.rows) && block.rows.length >= 1 && block.rows.length <= 100 && block.rows.every(row => Array.isArray(row) && row.length >= 1 && row.length <= 20 && row.length === (block.rows as unknown[][])[0].length && row.every(text)); break;
      }
      if (!valid) return null;
    }
    return root.blocks as Block[];
  } catch { return null; }
}
function fresh(type: BlockKind): Block {
  const id = crypto.randomUUID();
  switch (type) {
    case 'heading': return { id, type, text: '', level: 1 };
    case 'list': return { id, type, ordered: false, items: [''] };
    case 'checklist': return { id, type, items: [{ text: '', checked: false }] };
    case 'table': return { id, type, rows: [['', '']] };
    case 'divider': return { id, type };
    case 'link': return { id, type, text: '', url: '' };
    default: return { id, type, text: '' };
  }
}

export function DocumentBodyEditor({ body, mode, disabled, readOnly = false, onChange }: { body: string; mode: string; disabled: boolean; readOnly?: boolean; onChange: (body: string) => void }) {
  const [kind, setKind] = useState<BlockKind>('paragraph'), [preview, setPreview] = useState(false);
  const blocks = mode === 'Block' ? parse(body) : null;
  const bytes = new TextEncoder().encode(body).length;
  if (readOnly) return <DocumentBodyView body={body} mode={mode} />;
  function update(next: Block[]) { onChange(JSON.stringify({ schemaVersion: 1, blocks: next })); }
  function replace(index: number, block: Block) { if (blocks) update(blocks.map((current, i) => i === index ? block : current)); }
  function move(index: number, offset: number) { if (!blocks) return; const next = [...blocks]; [next[index], next[index + offset]] = [next[index + offset], next[index]]; update(next); }
  return <fieldset disabled={disabled} className="nested-panel"><legend>Nội dung · {mode || 'chọn editor mode'}</legend><p>{bytes.toLocaleString()} / 1.048.576 byte UTF-8 · Chỉ lưu khi bấm Save.</p>{bytes > 1048576 && <p role="alert">Nội dung vượt giới hạn 1 MiB.</p>}
    {mode !== 'Block' ? <><label htmlFor="document-body">Markdown<textarea id="document-body" rows={12} value={body} onChange={event => onChange(event.target.value)} /></label><button type="button" onClick={() => setPreview(value => !value)}>{preview ? 'Ẩn bản xem trước' : 'Xem trước Markdown'}</button>{preview && <MarkdownView body={body} />}<p>HTML được hiển thị như văn bản. Ảnh không được tự tải từ URL bên ngoài.</p></>
      : blocks === null ? <><p role="alert">Nội dung cũ hoặc định dạng chưa được hỗ trợ. Bản gốc được giữ nguyên; không tự chuyển đổi.</p><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{body}</pre>{!body.trimStart().startsWith('{') && !body.trimStart().startsWith('[') && <button type="button" onClick={() => update([{ id: crypto.randomUUID(), type: 'paragraph', text: body }])}>Chuyển văn bản cũ thành một paragraph trong bản nháp</button>}</>
        : <><button type="button" onClick={() => setPreview(value => !value)}>{preview ? 'Chỉnh sửa blocks' : 'Xem trước'}</button>{preview ? <BlockPreview blocks={blocks} /> : blocks.map((block, index) => <fieldset key={block.id} className="nested-panel"><legend>{index + 1}. {block.type}</legend>
          {'text' in block && <label>Nội dung<textarea value={block.text} rows={block.type === 'code' ? 6 : 3} onChange={event => replace(index, { ...block, text: event.target.value })} /></label>}
          {block.type === 'heading' && <label>Cấp tiêu đề<select value={block.level} onChange={event => replace(index, { ...block, level: Number(event.target.value) })}>{[1, 2, 3].map(level => <option key={level} value={level}>H{level}</option>)}</select></label>}
          {block.type === 'code' && <label>Ngôn ngữ<input value={block.language ?? ''} maxLength={64} onChange={event => replace(index, { ...block, language: event.target.value })} /></label>}
          {block.type === 'link' && <label>URL (http/https)<input type="url" maxLength={2048} value={block.url} onChange={event => replace(index, { ...block, url: event.target.value })} />{!safeUrl(block.url) && <span role="alert">Nhập URL http/https hợp lệ, không chứa thông tin đăng nhập, trước khi Save.</span>}</label>}
          {block.type === 'list' && <><label><input type="checkbox" checked={block.ordered} onChange={event => replace(index, { ...block, ordered: event.target.checked })} /> Đánh số</label>{block.items.map((item, itemIndex) => <div key={itemIndex}><label>Mục {itemIndex + 1}<textarea value={item} onChange={event => replace(index, { ...block, items: block.items.map((value, i) => i === itemIndex ? event.target.value : value) })} /></label><button type="button" onClick={() => replace(index, { ...block, items: block.items.filter((_, i) => i !== itemIndex) })}>Bỏ mục {itemIndex + 1}</button></div>)}<button type="button" disabled={block.items.length >= 1000} onClick={() => replace(index, { ...block, items: [...block.items, ''] })}>Thêm mục</button></>}
          {block.type === 'checklist' && <>{block.items.map((item, itemIndex) => <div key={itemIndex}><label><input type="checkbox" checked={item.checked} onChange={event => replace(index, { ...block, items: block.items.map((value, i) => i === itemIndex ? { ...value, checked: event.target.checked } : value) })} /> Mục {itemIndex + 1}</label><textarea aria-label={`Nội dung mục ${itemIndex + 1}`} value={item.text} onChange={event => replace(index, { ...block, items: block.items.map((value, i) => i === itemIndex ? { ...value, text: event.target.value } : value) })} /><button type="button" onClick={() => replace(index, { ...block, items: block.items.filter((_, i) => i !== itemIndex) })}>Bỏ mục {itemIndex + 1}</button></div>)}<button type="button" disabled={block.items.length >= 1000} onClick={() => replace(index, { ...block, items: [...block.items, { text: '', checked: false }] })}>Thêm mục</button></>}
          {block.type === 'table' && <><table><caption>Nội dung bảng</caption><tbody>{block.rows.map((row, r) => <tr key={r}>{row.map((cell, c) => <td key={c}><textarea aria-label={`Hàng ${r + 1}, cột ${c + 1}`} value={cell} onChange={event => replace(index, { ...block, rows: block.rows.map((values, i) => i === r ? values.map((value, j) => j === c ? event.target.value : value) : values) })} /></td>)}</tr>)}</tbody></table><button type="button" disabled={block.rows.length >= 100} onClick={() => replace(index, { ...block, rows: [...block.rows, block.rows[0].map(() => '')] })}>Thêm hàng</button><button type="button" disabled={block.rows.length <= 1} onClick={() => replace(index, { ...block, rows: block.rows.slice(0, -1) })}>Bỏ hàng cuối</button><button type="button" disabled={block.rows[0].length >= 20} onClick={() => replace(index, { ...block, rows: block.rows.map(row => [...row, '']) })}>Thêm cột</button><button type="button" disabled={block.rows[0].length <= 1} onClick={() => replace(index, { ...block, rows: block.rows.map(row => row.slice(0, -1)) })}>Bỏ cột cuối</button></>}
          <div className="form-actions"><button type="button" disabled={index === 0} onClick={() => move(index, -1)}>Lên</button><button type="button" disabled={index === blocks.length - 1} onClick={() => move(index, 1)}>Xuống</button><button type="button" onClick={() => update(blocks.filter((_, i) => i !== index))}>Bỏ block</button></div></fieldset>)}<label>Loại block<select value={kind} onChange={event => setKind(event.target.value as BlockKind)}>{kinds.map(type => <option key={type} value={type}>{type}</option>)}</select></label><button type="button" disabled={blocks.length >= 1000} onClick={() => update([...blocks, fresh(kind)])}>Thêm block</button><p>Ảnh và cover cần tích hợp tham chiếu Files; chưa khả dụng trong editor này.</p></>}
  </fieldset>;
}

/** Pure projection: never obtains owner/source data or files in another access context. */
export function DocumentBodyView({ body, mode }: { body: string; mode: string }) {
  const blocks = mode === 'Block' ? parse(body) : null;
  return <section className="nested-panel" aria-label="Nội dung chỉ đọc"><h3>Nội dung · chỉ đọc</h3>{mode === 'Block' ? blocks ? <BlockPreview blocks={blocks} /> : <pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{body}</pre> : mode === 'Markdown' ? <MarkdownView body={body} /> : <pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{body}</pre>}</section>;
}

function BlockPreview({ blocks }: { blocks: Block[] }) {
  return <div style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{blocks.map(block => {
    switch (block.type) {
      case 'paragraph': return <p key={block.id}>{block.text}</p>;
      case 'quote': return <blockquote key={block.id}>{block.text}</blockquote>;
      case 'heading': return block.level === 1 ? <h1 key={block.id}>{block.text}</h1> : block.level === 2 ? <h2 key={block.id}>{block.text}</h2> : <h3 key={block.id}>{block.text}</h3>;
      case 'divider': return <hr key={block.id} />;
      case 'code': return <pre key={block.id}><code>{block.text}</code></pre>;
      case 'link': return <p key={block.id}>{block.text}{safeUrl(block.url) ? ` (${block.url})` : ''}</p>;
      case 'list': { const items = block.items.map((item, i) => <li key={i}>{item}</li>); return block.ordered ? <ol key={block.id}>{items}</ol> : <ul key={block.id}>{items}</ul>; }
      case 'checklist': return <ul key={block.id}>{block.items.map((item, i) => <li key={i}>{item.checked ? '☑' : '☐'} {item.text}</li>)}</ul>;
      case 'table': return <table key={block.id}><tbody>{block.rows.map((row, i) => <tr key={i}>{row.map((cell, j) => <td key={j}>{cell}</td>)}</tr>)}</tbody></table>;
    }
  })}</div>;
}
