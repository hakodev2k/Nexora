import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, test } from 'vitest';
import { DocumentVersionDiff } from './DocumentVersionDiff';
import type { DocumentVersionPreview } from './api';
const version = (number: number, body: string, title = 'Original'): DocumentVersionPreview => ({ body, snapshot: {
  id: String(number), pageId: 'page', versionNumber: number, title, documentType: 'Note', editorMode: 'Markdown', savedStatus: 'Draft', changeNote: null, createdAt: '2026-10-22T00:00:00Z', createdByUserId: null, tagSnapshotCaptured: true, tagId: null, tagName: null
} });
test('differences and full snapshots remain inert text and never create active media', async () => {
  const user = userEvent.setup();
  const before = version(1, '<script>window.canary=1</script>\n'), after = version(2, '<img src="https://example.invalid/private">\n', '<b>Edited</b>');
  const { container } = render(<DocumentVersionDiff before={before} after={after} />);
  expect(screen.getByRole('article', { name: 'Đã bỏ' })).toHaveTextContent(before.body.trim());
  expect(screen.getByRole('article', { name: 'Đã thêm' })).toHaveTextContent(after.body.trim());
  expect(container.querySelectorAll('img,script,iframe,a')).toHaveLength(0);
  expect(screen.getByText('Sau: <b>Edited</b>')).toBeInTheDocument();
  const summary = screen.getByText('Xem toàn bộ hai snapshot'); await user.click(summary);
  expect(summary.closest('details')).toHaveAttribute('open');
});
test('unchanged body is identified even when metadata changed', () => {
  render(<DocumentVersionDiff before={version(1, 'same')} after={version(2, 'same', 'New title')} />);
  expect(screen.getByText('Nội dung hai phiên bản giống nhau.')).toBeInTheDocument();
  expect(screen.getByText('Sau: New title')).toBeInTheDocument();
});
test('large source explicitly reports simplified comparison', () => {
  render(<DocumentVersionDiff before={version(1, 'a\n'.repeat(401))} after={version(2, 'b\n'.repeat(401))} />);
  expect(screen.getByText(/Nội dung lớn:/)).toBeInTheDocument();
});
