import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { useId } from 'react';

function safeHref(value: string) {
  if (/^#[A-Za-z0-9_:.%-]+$/.test(value)) return value;
  if (/[\u0000-\u001f\u007f]/.test(value) || value.length > 2048) return '';
  try { const url = new URL(value); return ['https:', 'http:'].includes(url.protocol) && !url.username && !url.password ? value : ''; } catch { return ''; }
}

/** No raw HTML plugin, injected HTML, remote image fetch or executable URL. */
export default function DocumentMarkdownPreview({ body }: { body: string }) {
  const prefix = useId();
  return <div className="document-preview"><Markdown remarkPlugins={[remarkGfm]} remarkRehypeOptions={{ clobberPrefix: `document-${prefix}-` }} urlTransform={(url, key) => key === 'href' ? safeHref(url) : ''}
    components={{
      a: ({ href, children }) => href?.startsWith('#') && safeHref(href) ? <a href={href}>{children}</a> : <span>{children}{href ? ` (${href})` : ''}</span>,
      img: ({ alt }) => <span role="img" aria-label={alt || 'Ảnh'}>[Ảnh: {alt || 'không có mô tả'} — cần tham chiếu Files]</span>,
      input: ({ checked }) => <input type="checkbox" checked={Boolean(checked)} disabled readOnly aria-label={checked ? 'Hoàn thành' : 'Chưa hoàn thành'} />
    }}>{body}</Markdown></div>;
}
