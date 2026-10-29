import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { expect, it, vi } from 'vitest';
import { SkillEvidencePanel } from './SkillEvidence';
import { SkillScreen } from './SkillScreen';
import type { Skill } from './skillApi';
import { server } from './test/server';

const source: Skill = { id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', title: 'Source skill', level: 'Beginner', description: null, category: null, lastUsed: null, status: 'Active', createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"source-v1"' };
const target: Skill = { ...source, id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', title: 'Target skill', level: 'Expert', etag: '"target-v1"' };
const course = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const root = '*/api/v1/learning/skills';
function csrf() { server.use(http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf', tokenType: 'csrf', expiresInSeconds: 1800 }))); }

it('Skill evidence HTTP408 freezes intent and retries exact body/key/ETag', async () => {
  csrf(); const commands: { body: string; key: string | null; etag: string | null }[] = [];
  server.use(
    http.get(root + '/:id/evidence', () => HttpResponse.json({ items: [], nextCursor: null, skillETag: source.etag })),
    http.get('*/api/v1/learning/courses', () => HttpResponse.json({ items: [{ id: course, title: 'Synthetic course' }], nextCursor: null })),
    http.post(root + '/:id/evidence', async ({ request }) => {
      commands.push({ body: await request.text(), key: request.headers.get('Idempotency-Key'), etag: request.headers.get('If-Match') });
      return commands.length === 1 ? HttpResponse.json({ code: 'Timeout', title: 'Uncertain408' }, { status: 408 }) : HttpResponse.json({ id: source.id, committed: true, detailAvailable: false });
    })
  );
  const user = userEvent.setup(); render(<SkillEvidencePanel skill={source} canWrite onClose={vi.fn()} onFailure={vi.fn()} />);
  await user.click(screen.getByRole('button', { name: 'Tải evidence hiện tại' })); await screen.findByText('Chưa có evidence.');
  await user.click(screen.getByRole('button', { name: 'Chọn từ Course hiện tại' })); await screen.findByRole('option', { name: 'Synthetic course' });
  await user.selectOptions(screen.getByRole('combobox', { name: /^Course/ }), course); await user.type(screen.getByRole('textbox', { name: /^Ghi chú/ }), 'Original private note');
  await user.click(screen.getByRole('button', { name: 'Ghi evidence' })); await screen.findByText('Uncertain408');
  expect(screen.getByRole('textbox', { name: /^Ghi chú/ })).toBeDisabled();
  await user.click(screen.getByRole('button', { name: 'Thử lại đúng request/mã cũ' })); await screen.findByText('Đã ghi thay đổi. Tải lại để đọc evidence và revision hiện tại.');
  expect(commands).toHaveLength(2); expect(commands[0].key).toBeTruthy(); expect(commands[1]).toEqual(commands[0]); expect(commands[0].etag).toBe(source.etag);
});

it('Skill merge HTTP408 retains immutable preview and retries exact body/key/ETag', async () => {
  csrf(); const commands: { body: string; key: string | null; etag: string | null }[] = [];
  server.use(
    http.get(root + '/capabilities', () => HttpResponse.json({ 'learning.skill.read': true, 'learning.skill.merge': true })),
    http.get(root, () => HttpResponse.json({ items: [source, target], nextCursor: null })),
    http.post(root + '/:id/preview-merge', () => HttpResponse.json({ source, target, sourceEvidenceCount: 1, targetEvidenceCount: 1, overlapCount: 1, addedEvidenceCount: 0, expiresAt: '2026-10-29T10:00:00Z', previewToken: 'synthetic-preview' })),
    http.post(root + '/:id/merge', async ({ request }) => {
      commands.push({ body: await request.text(), key: request.headers.get('Idempotency-Key'), etag: request.headers.get('If-Match') });
      return commands.length === 1 ? HttpResponse.json({ code: 'Timeout', title: 'Uncertain408' }, { status: 408 }) : HttpResponse.json({ id: target.id, committed: true, detailAvailable: false });
    })
  );
  const user = userEvent.setup(); render(<SkillScreen onAuthLost={vi.fn()} />);
  const title = await screen.findByRole('heading', { name: source.title }); const card = title.closest('article'); expect(card).not.toBeNull();
  await user.click(within(card!).getByRole('button', { name: 'Merge into another Skill' }));
  await user.click(await screen.findByRole('button', { name: 'Preview merge into Target skill' }));
  await user.click(await screen.findByRole('button', { name: 'Confirm merge' })); await screen.findAllByText('Uncertain408');
  const retry = await screen.findByRole('button', { name: 'Thử lại đúng request/mã cũ' });
  expect(screen.getByRole('dialog')).toHaveTextContent('"source-v1"'); expect(screen.getByRole('dialog')).toHaveTextContent('"target-v1"');
  await user.click(retry); expect(commands).toHaveLength(2); expect(commands[0].key).toBeTruthy(); expect(commands[1]).toEqual(commands[0]);
});
