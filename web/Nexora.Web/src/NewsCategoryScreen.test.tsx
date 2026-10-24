import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { NewsCategoryScreen } from './NewsCategoryScreen';
import * as api from './newsCategoryApi';
vi.mock('./newsCategoryApi');
const caps = Object.fromEntries(['read', 'create', 'update'].map(name => ['news.category.' + name, true]));
const item: api.NewsCategoryRecord = { id: 'private-category', metadata: { schemaVersion: 1, name: 'Private literal category' }, createdAt: '2026-10-06T00:00:00Z', updatedAt: '2026-10-06T00:00:00Z', etag: 'original-etag' };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.newsCategoryCapabilities).mockResolvedValue(caps);
  vi.mocked(api.newsCategoryList).mockResolvedValue({ items: [item], nextCursor: null });
  vi.mocked(api.newsCategoryGet).mockResolvedValue(item);
});
async function edit() {
  const user = userEvent.setup(); render(<NewsCategoryScreen onAuthLost={vi.fn()} />);
  await user.click(await screen.findByText('Edit category')); return user;
}
describe('News category authority and recovery', () => {
  it('retains a dirty draft when Escape discard is cancelled and removes unload protection after confirmed discard', async () => {
    const user = await edit(); await user.clear(screen.getByLabelText('Category name')); await user.type(screen.getByLabelText('Category name'), 'Uncommitted draft');
    const before = new Event('beforeunload', { cancelable: true }); window.dispatchEvent(before); expect(before.defaultPrevented).toBe(true);
    await user.keyboard('{Escape}'); await user.click(await screen.findByText('Tiếp tục sửa')); expect(screen.getByLabelText('Category name')).toHaveValue('Uncommitted draft');
    await user.keyboard('{Escape}'); await user.click(await screen.findByText('Bỏ bản nháp')); await waitFor(() => expect(screen.queryByLabelText('Category name')).toBeNull());
    const after = new Event('beforeunload', { cancelable: true }); window.dispatchEvent(after); expect(after.defaultPrevented).toBe(false); expect(api.newsCategoryUpdate).not.toHaveBeenCalled();
  });
  it('does not load private rows after a pending capability lookup resolves following unmount', async () => {
    let resolve!: (value: Record<string, boolean>) => void;
    vi.mocked(api.newsCategoryCapabilities).mockReturnValue(new Promise(done => { resolve = done; }));
    const view = render(<NewsCategoryScreen onAuthLost={vi.fn()} />); view.unmount(); resolve(caps);
    await new Promise(done => setTimeout(done, 0)); expect(api.newsCategoryList).not.toHaveBeenCalled();
  });
  it('serializes cursor fetches and deduplicates overlapping IDs', async () => {
    let resolve!: (value: api.NewsCategoryPage) => void;
    vi.mocked(api.newsCategoryList).mockResolvedValueOnce({ items: [item], nextCursor: 'owned-boundary' }).mockReturnValueOnce(new Promise(done => { resolve = done; }));
    const user = userEvent.setup(); render(<NewsCategoryScreen onAuthLost={vi.fn()} />);
    await user.dblClick(await screen.findByText('Load more categories'));
    expect(api.newsCategoryList).toHaveBeenCalledTimes(2); expect(vi.mocked(api.newsCategoryList).mock.calls[1]).toEqual(['', 'owned-boundary']);
    resolve({ items: [item, { ...item, id: 'second-category', metadata: { ...item.metadata, name: 'Second page category' } }], nextCursor: null });
    await screen.findByText('Second page category'); expect(screen.getAllByRole('article')).toHaveLength(2); expect(screen.queryByText('Load more categories')).toBeNull();
  });
  it.each([0, 503, 409])('freezes and exactly retries an uncertain update with status %s', async status => {
    vi.mocked(api.newsCategoryUpdate).mockRejectedValueOnce(Object.assign(new Error('Unknown outcome'), { status, code: status === 409 ? 'RequestInProgress' : undefined })).mockResolvedValueOnce({ id: item.id, etag: 'ack-etag' });
    const user = await edit(); await user.clear(screen.getByLabelText('Category name')); await user.type(screen.getByLabelText('Category name'), 'Retained draft');
    await user.click(screen.getByText('Save category')); await screen.findByText('Unknown outcome'); expect(screen.getByLabelText('Category name')).toBeDisabled();
    await user.click(screen.getByText('Retry same save')); await waitFor(() => expect(api.newsCategoryUpdate).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.newsCategoryUpdate).mock.calls[1]).toEqual(vi.mocked(api.newsCategoryUpdate).mock.calls[0]);
    await waitFor(() => expect(screen.queryByLabelText('Category name')).toBeNull());
  });
  it('warns about possible server commit before discarding an uncertain save', async () => {
    vi.mocked(api.newsCategoryUpdate).mockRejectedValueOnce(Object.assign(new Error('Unknown outcome'), { status: 503 }));
    const user = await edit(); await user.click(screen.getByText('Save category')); await screen.findByText('Unknown outcome');
    await user.keyboard('{Escape}'); await screen.findByText('The save outcome is unknown. Discarding leaves any committed server category intact.');
    expect(api.newsCategoryUpdate).toHaveBeenCalledTimes(1);
  });
  it.each([0, 503, 409])('exactly retries uncertain creation with status %s without creating a fresh command', async status => {
    vi.mocked(api.newsCategoryCreate).mockRejectedValueOnce(Object.assign(new Error('Unknown creation'), { status, code: status === 409 ? 'RequestInProgress' : undefined })).mockResolvedValueOnce({ id: 'created-category', etag: 'ack-etag' });
    const user = userEvent.setup(); render(<NewsCategoryScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('New category')); await user.type(screen.getByLabelText('Category name'), 'Frozen new category'); await user.click(screen.getByText('Save category'));
    await screen.findByText('Unknown creation'); expect(screen.getByLabelText('Category name')).toBeDisabled();
    await user.click(screen.getByText('Retry same save')); await waitFor(() => expect(api.newsCategoryCreate).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.newsCategoryCreate).mock.calls[1]).toEqual(vi.mocked(api.newsCategoryCreate).mock.calls[0]);
  });
  it.each([401, 403, 404])('clears private records and editor on current save denial %s', async status => {
    vi.mocked(api.newsCategoryUpdate).mockRejectedValueOnce(Object.assign(new Error('Unavailable'), { status }));
    const user = await edit(); await user.click(screen.getByText('Save category')); await screen.findByText('Unavailable');
    expect(screen.queryByText(item.metadata.name)).toBeNull(); expect(screen.queryByLabelText('Category name')).toBeNull();
  });
  it('requires comparison again when the current revision changes before explicit reapply', async () => {
    const b = { ...item, etag: 'revision-b', metadata: { ...item.metadata, name: 'Concurrent B' } };
    const c = { ...item, etag: 'revision-c', metadata: { ...item.metadata, name: 'Concurrent C' } };
    vi.mocked(api.newsCategoryGet).mockResolvedValueOnce(b).mockResolvedValue(c);
    vi.mocked(api.newsCategoryUpdate).mockRejectedValueOnce(Object.assign(new Error('Stale'), { status: 412 })).mockResolvedValueOnce({ id: item.id, etag: 'ack-etag' });
    const user = await edit(); await user.clear(screen.getByLabelText('Category name')); await user.type(screen.getByLabelText('Category name'), 'My draft');
    await user.click(screen.getByText('Save category')); await screen.findByText('Concurrent B'); expect(api.newsCategoryUpdate).toHaveBeenCalledTimes(1);
    await user.click(screen.getByText('Reapply draft')); await screen.findByText('Concurrent C'); expect(screen.getByText('Save category')).toBeDisabled();
    await user.click(screen.getByText('Reapply draft')); expect(screen.getByLabelText('Category name')).toHaveValue('My draft');
    await user.click(screen.getByText('Save category')); await waitFor(() => expect(api.newsCategoryUpdate).toHaveBeenCalledTimes(2));
    const calls = vi.mocked(api.newsCategoryUpdate).mock.calls; expect(calls[1][0].etag).toBe('revision-c'); expect(calls[1][2]).not.toBe(calls[0][2]);
  });
  it('creates with an opaque acknowledgement without requesting hidden read authority', async () => {
    vi.mocked(api.newsCategoryCapabilities).mockResolvedValue({ ...caps, 'news.category.read': false, 'news.category.update': false });
    vi.mocked(api.newsCategoryCreate).mockResolvedValue({ id: 'created-private-id', etag: 'ack-etag' });
    const user = userEvent.setup(); render(<NewsCategoryScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('New category')); await user.type(screen.getByLabelText('Category name'), 'Create-only draft'); await user.click(screen.getByText('Save category'));
    await waitFor(() => expect(api.newsCategoryCreate).toHaveBeenCalledTimes(1)); await waitFor(() => expect(screen.queryByLabelText('Category name')).toBeNull());
    expect(api.newsCategoryList).not.toHaveBeenCalled(); expect(api.newsCategoryGet).not.toHaveBeenCalled(); expect(screen.queryByText('Create-only draft')).toBeNull();
  });
  it('distinguishes unavailable capabilities from an authorized empty list', async () => {
    vi.mocked(api.newsCategoryCapabilities).mockResolvedValue(Object.fromEntries(Object.keys(caps).map(key => [key, false]))); render(<NewsCategoryScreen onAuthLost={vi.fn()} />);
    await screen.findByText('News categories are unavailable for the current module or permissions.'); expect(screen.queryByText('No saved categories.')).toBeNull(); expect(api.newsCategoryList).not.toHaveBeenCalled();
  });
});
