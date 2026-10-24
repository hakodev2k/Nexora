import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MonitoringScreen } from './MonitoringScreen';
import * as api from './monitorApi';
vi.mock('./monitorApi');
const caps = Object.fromEntries(['read', 'create', 'update', 'pause', 'resume'].map(name => ['monitoring.monitor.' + name, true]));
const item: api.MonitorRecord = { id: 'private-monitor', metadata: { schemaVersion: 1, title: 'Private literal title', kind: 'Http', target: 'https://monitor.example/health', intervalSeconds: 60, expectedStatus: null }, enabled: true, state: 'Unknown', lastObservedAt: null, createdAt: '2026-10-04T00:00:00Z', updatedAt: '2026-10-04T00:00:00Z', etag: 'original-etag' };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.monitorCapabilities).mockResolvedValue(caps);
  vi.mocked(api.monitorList).mockResolvedValue({ items: [item], nextCursor: null });
  vi.mocked(api.monitorGet).mockResolvedValue(item);
});
async function edit() {
  const user = userEvent.setup(); render(<MonitoringScreen onAuthLost={vi.fn()} />);
  await user.click(await screen.findByText('Edit Monitor'));
  return user;
}
describe('Monitoring authority and command recovery', () => {
  it('does not fetch protected rows after unmount while capability lookup is pending', async () => {
    let resolve!: (value: Record<string, boolean>) => void;
    vi.mocked(api.monitorCapabilities).mockReturnValue(new Promise(done => { resolve = done; }));
    const view = render(<MonitoringScreen onAuthLost={vi.fn()} />); view.unmount(); resolve(caps);
    await new Promise(done => setTimeout(done, 0)); expect(api.monitorList).not.toHaveBeenCalled();
  });
  it('serializes real cursor loading and appends the next page without duplicating a request', async () => {
    let resolve!: (value: api.MonitorPage) => void;
    vi.mocked(api.monitorList).mockResolvedValueOnce({ items: [item], nextCursor: 'next-owned-boundary' }).mockReturnValueOnce(new Promise(done => { resolve = done; }));
    const user = userEvent.setup(); render(<MonitoringScreen onAuthLost={vi.fn()} />);
    await user.dblClick(await screen.findByText('Load more Monitor items'));
    expect(api.monitorList).toHaveBeenCalledTimes(2);
    expect(vi.mocked(api.monitorList).mock.calls[1]).toEqual(['', '', 'next-owned-boundary']);
    resolve({ items: [{ ...item, id: 'second-owned-monitor', metadata: { ...item.metadata, title: 'Second page title' } }], nextCursor: null });
    await screen.findByText('Second page title'); expect(screen.getAllByRole('article')).toHaveLength(2);
  });
  it('freezes metadata and reuses the exact command after an unknown save outcome', async () => {
    vi.mocked(api.monitorUpdate).mockRejectedValueOnce(Object.assign(new Error('Unknown outcome'), { status: 503 })).mockResolvedValueOnce(item);
    const user = await edit();
    await user.clear(screen.getByLabelText('Title')); await user.type(screen.getByLabelText('Title'), 'Retained draft');
    await user.click(screen.getByText('Save Monitor')); await screen.findByText('Unknown outcome');
    expect(screen.getByLabelText('Title')).toBeDisabled();
    await user.click(screen.getByText('Retry same save'));
    await waitFor(() => expect(api.monitorUpdate).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.monitorUpdate).mock.calls[1]).toEqual(vi.mocked(api.monitorUpdate).mock.calls[0]);
  });
  it('clears protected records and drafts when an edited source becomes unavailable', async () => {
    vi.mocked(api.monitorUpdate).mockRejectedValue(Object.assign(new Error('Unavailable'), { status: 404 }));
    const user = await edit(); await user.click(screen.getByText('Save Monitor'));
    await screen.findByText('Unavailable');
    expect(screen.queryByText(item.metadata.title)).toBeNull();
    expect(screen.queryByLabelText('Target URL')).toBeNull();
  });
  it('requires comparison of a newer revision before explicitly retaining and saving the draft', async () => {
    const current = { ...item, etag: 'current-etag', metadata: { ...item.metadata, title: 'Concurrent title' } };
    vi.mocked(api.monitorGet).mockResolvedValue(current);
    vi.mocked(api.monitorUpdate).mockRejectedValueOnce(Object.assign(new Error('Stale'), { status: 412 })).mockResolvedValueOnce(current);
    const user = await edit(); await user.clear(screen.getByLabelText('Title')); await user.type(screen.getByLabelText('Title'), 'My draft');
    await user.click(screen.getByText('Save Monitor')); await screen.findByText('Concurrent title');
    expect(screen.getByText('Save Monitor')).toBeDisabled();
    await user.click(screen.getByText('Reapply draft'));
    expect(screen.getByLabelText('Title')).toHaveValue('My draft');
    await user.click(screen.getByText('Save Monitor'));
    await waitFor(() => expect(api.monitorUpdate).toHaveBeenCalledTimes(2));
    const calls = vi.mocked(api.monitorUpdate).mock.calls;
    expect(calls[1][0].etag).toBe('current-etag'); expect(calls[1][2]).not.toBe(calls[0][2]);
  });
  it('removes saved metadata when refreshed read authority disappears', async () => {
    const user = userEvent.setup(); render(<MonitoringScreen onAuthLost={vi.fn()} />);
    await screen.findByText(item.metadata.title);
    vi.mocked(api.monitorCapabilities).mockResolvedValue({ ...caps, 'monitoring.monitor.read': false });
    await user.click(screen.getByText('Tải lại'));
    await waitFor(() => expect(screen.queryByText(item.metadata.title)).toBeNull());
    expect(screen.queryByText('Edit Monitor')).toBeNull();
  });
});
