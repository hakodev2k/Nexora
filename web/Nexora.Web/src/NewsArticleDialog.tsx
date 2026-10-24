import { useEffect, useRef, useState } from 'react';
import { newsArticle, type NewsArticle } from './newsReaderApi';
import { ResourceFormDialog } from './ResourceFormDialog';

export function NewsArticleDialog({ articleId, onClose, onAuthLost, onUnavailable }: { articleId: string; onClose: () => void; onAuthLost: () => Promise<void>; onUnavailable: (status?: number) => void }) {
  const callbacks = useRef({ onUnavailable }); callbacks.current = { onUnavailable };
  const [article, setArticle] = useState<NewsArticle | null>(null), [loading, setLoading] = useState(true), [error, setError] = useState<string | null>(null);
  useEffect(() => {
    let cancelled = false; setArticle(null); setLoading(true); setError(null);
    newsArticle(articleId).then(value => { if (!cancelled) setArticle(value); }).catch(value => {
      if (cancelled) return; setArticle(null); setError(value instanceof Error ? value.message : 'Nguồn không khả dụng.');
      callbacks.current.onUnavailable((value as { status?: number }).status);
      if ((value as { status?: number }).status === 401) void onAuthLost();
    }).finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [articleId, onAuthLost]);
  return <ResourceFormDialog open title="News · Internal reader" busy={false} dirty={false} onClose={onClose}>
    {loading && <p role="status">Đang kiểm tra nguồn hiện tại…</p>}{error && <p role="alert">{error}</p>}
    {article && <article><h2>{article.title}</h2><p>{article.sourceTitle} · {article.publishedAt ?? article.fetchedAt}</p><p>{article.isExcerpt ? 'Excerpt do feed cung cấp' : 'Nội dung đã lưu do feed cung cấp'}</p><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{article.body ?? 'Feed không có nội dung để đọc.'}</pre><p>Attribution URL: {article.originalUrl}</p><p>{article.read ? 'Read' : 'Unread'} · trạng thái được đọc lại từ News.</p></article>}
  </ResourceFormDialog>;
}
