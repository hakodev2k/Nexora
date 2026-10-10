import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CalendarExportScreen } from './CalendarExportScreen';
import * as api from './calendarExportApi';
vi.mock('./calendarExportApi');
const filter: api.ExportFilter = { schemaVersion: 1, sourceKinds: ['Manual'], manualStatuses: ['Scheduled'], taskStatuses: [], range: null, containmentMode: 'FullyContained' };
const caps = { 'transfer.export.request': true, 'transfer.export.read': true, 'transfer.export.download': true, 'source.Manual': true, 'source.Task': true };
const job: api.ExportJob = { id: 'opaque-job', state: 'Ready', count: 2, filter, timeZoneId: 'UTC', createdAt: '2026-10-04T00:00:00Z', expiresAt: '2026-10-04T00:15:00Z', etag: 'opaque-etag' };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.exportCapabilities).mockResolvedValue(caps);
  vi.mocked(api.exportJobs).mockResolvedValue({ items: [job], nextCursor: null });
  vi.mocked(api.exportJob).mockResolvedValue(job);
  vi.mocked(api.exportPreview).mockResolvedValue({ count: 2, timeZoneId: 'UTC', previewToken: 'opaque-preview', expiresAt: '2026-10-04T00:02:00Z' });
});
async function openEditor() {
  const user = userEvent.setup(); render(<CalendarExportScreen onAuthLost={vi.fn()} />);
  await waitFor(() => expect(screen.getByText('Chọn phạm vi export')).toBeEnabled());
  await user.click(screen.getByText('Chọn phạm vi export')); return user;
}
describe('Calendar export authorization and recovery', () => {
  it('allows Task-only selection when Manual read is unavailable without choosing a source automatically', async () => {
    vi.mocked(api.exportCapabilities).mockResolvedValue({ ...caps, 'source.Manual': false });
    const user = await openEditor();
    expect(screen.getByLabelText('Manual Events')).toBeEnabled();
    await user.click(screen.getByLabelText('Manual Events')); await user.click(screen.getByLabelText('Task Events'));
    await user.click(screen.getByText('Preview export'));
    await screen.findByRole('article', { name: 'Export preview' });
    expect(api.exportPreview).toHaveBeenCalledWith({ ...filter, sourceKinds: ['Task'], manualStatuses: [], taskStatuses: ['NotStarted'] });
    expect(api.requestExport).not.toHaveBeenCalled();
  });
  it('clears a selected report on Reload when its individual source is no longer listed', async () => {
    const user = userEvent.setup(); render(<CalendarExportScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Mở export report'));
    await screen.findByRole('article', { name: 'Export report' });
    vi.mocked(api.exportJobs).mockResolvedValue({ items: [], nextCursor: null });
    await user.click(screen.getByText('Tải lại'));
    await waitFor(() => expect(screen.queryByRole('article', { name: 'Export report' })).toBeNull());
    expect(screen.queryByText('Download ICS')).toBeNull();
  });
  it('clears protected cards when a source report becomes unavailable', async () => {
    vi.mocked(api.exportJob).mockRejectedValue(Object.assign(new Error('Unavailable'), { status: 404 }));
    const user = userEvent.setup(); render(<CalendarExportScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Mở export report'));
    await screen.findByRole('alert');
    expect(screen.queryByText('ICS · 2 Events')).toBeNull();
    expect(screen.queryByRole('article', { name: 'Export report' })).toBeNull();
  });
  it('retains the confirmed command key after a lost acknowledgment and uses explicit retry', async () => {
    vi.mocked(api.requestExport).mockRejectedValueOnce(Object.assign(new Error('Retryable'), { status: 503 })).mockResolvedValueOnce({ jobId: job.id, state: job.state, count: job.count, createdAt: job.createdAt, expiresAt: job.expiresAt, etag: job.etag });
    const user = await openEditor(); await user.click(screen.getByText('Preview export'));
    await user.click(await screen.findByText('Generate export')); await user.click(screen.getByText('Xác nhận Generate'));
    await screen.findByText('Retryable');
    await waitFor(() => expect(screen.getByText('Xác nhận Generate')).toBeEnabled());
    await user.click(screen.getByText('Xác nhận Generate'));
    await screen.findByRole('article', { name: 'Export report' });
    const calls = vi.mocked(api.requestExport).mock.calls;
    expect(calls).toHaveLength(2); expect(calls[1]).toEqual(calls[0]);
  });
});
