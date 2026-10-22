import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, test, vi } from 'vitest';
import { FocusScreen } from './FocusScreen';
import { TimeTrackingScreen } from './TimeTrackingScreen';
import * as time from './timeApi';
import * as focus from './focusApi';

vi.mock('./timeApi', () => ({ timeCapabilities: vi.fn(), timeEntries: vi.fn(), timeTimer: vi.fn(), timeReport: vi.fn(), timeHistory: vi.fn(), timeSave: vi.fn(), timeStart: vi.fn(), timeStop: vi.fn(), timeTransition: vi.fn(), timePreviewPurge: vi.fn(), timePurge: vi.fn() }));
vi.mock('./focusApi', () => ({ focusCapabilities: vi.fn(), focusList: vi.fn(), focusPreferences: vi.fn(), focusSavePreferences: vi.fn(), focusStart: vi.fn(), focusTransition: vi.fn(), focusRecordTime: vi.fn() }));
const entry = { id: 'entry-a', startAt: '2026-10-22T07:00:00Z', endAt: '2026-10-22T08:00:00Z', description: 'Private time row', category: null, status: 'Stopped', durationMilliseconds: 3600000, updatedAt: '2026-10-22T08:00:00Z', etag: '"one"' };
const prefs = { focusMinutes: 25, shortBreakMinutes: 5, longBreakMinutes: 15, cycleLength: 4, etag: '"one"' };
const timeCaps = { 'time.entry.read': true, 'time.entry.history': true, 'time.entry.create': true, 'time.entry.update': true, 'time.entry.purge': true };
const focusCaps = { 'focus.session.read': true, 'focus.preference.update': true, 'focus.session.start': true, 'focus.session.cancel': true, 'focus.session.record_time': true };
const deferred = <T,>() => { let resolve!: (v: T) => void; const promise = new Promise<T>(r => { resolve = r; }); return { promise, resolve }; };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(time.timeCapabilities).mockResolvedValue(timeCaps);
  vi.mocked(time.timeEntries).mockResolvedValue({ items: [entry], nextCursor: null });
  vi.mocked(time.timeTimer).mockResolvedValue(null);
  vi.mocked(time.timeReport).mockResolvedValue({ grossDurationMilliseconds: 0, entryCount: 0, hasOverlaps: false });
  vi.mocked(time.timeHistory).mockResolvedValue({ items: [], nextCursor: null });
  vi.mocked(time.timeSave).mockResolvedValue(entry);
  vi.mocked(focus.focusCapabilities).mockResolvedValue(focusCaps);
  vi.mocked(focus.focusList).mockResolvedValue({ items: [], nextCursor: null });
  vi.mocked(focus.focusPreferences).mockResolvedValue(prefs);
  vi.mocked(focus.focusSavePreferences).mockResolvedValue(prefs);
});
async function openTime() { const user = userEvent.setup(); render(<TimeTrackingScreen onAuthLost={vi.fn()} />); await waitFor(() => expect(screen.getByRole('button', { name: 'Manual entry' })).toBeEnabled()); await user.click(screen.getByRole('button', { name: 'Manual entry' })); return user; }

test('Time dirty Cancel preserves draft until explicit discard', async () => {
  const user = await openTime(); await user.type(screen.getByLabelText('Description'), 'Draft content');
  await user.click(screen.getByRole('button', { name: 'Hủy' }));
  expect(screen.getByText('Bỏ thay đổi chưa lưu?')).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Tiếp tục sửa' }));
  expect(screen.getByLabelText('Description')).toHaveValue('Draft content'); expect(time.timeSave).not.toHaveBeenCalled();
});
test('Time invalid date shows accessible error and does not write', async () => {
  const user = await openTime(); fireEvent.change(screen.getByLabelText('End (múi giờ trình duyệt)'), { target: { value: '2020-01-01T00:00' } });
  await user.click(screen.getByRole('button', { name: 'Save' }));
  expect(within(screen.getByRole('dialog')).getByRole('alert')).toHaveTextContent('Giờ kết thúc phải sau giờ bắt đầu.'); expect(time.timeSave).not.toHaveBeenCalled();
});
test('Time fields lock while save is unresolved', async () => {
  const pending = deferred<typeof entry>(); vi.mocked(time.timeSave).mockReturnValue(pending.promise);
  const user = await openTime(); await user.click(screen.getByRole('button', { name: 'Save' }));
  expect(screen.getByLabelText('Description')).toBeDisabled(); expect(screen.getByLabelText('Category')).toBeDisabled(); expect(screen.getByRole('button', { name: 'Hủy' })).toBeDisabled();
  await act(async () => { pending.resolve(entry); });
});
test('late Time history cannot reopen after read capability is revoked', async () => {
  const pending = deferred<{ items: time.TimeCorrection[]; nextCursor: string | null }>(); vi.mocked(time.timeHistory).mockReturnValue(pending.promise);
  const user = userEvent.setup(); render(<TimeTrackingScreen onAuthLost={vi.fn()} />); await user.click(await screen.findByRole('button', { name: 'History' }));
  vi.mocked(time.timeCapabilities).mockResolvedValue({}); await user.click(screen.getByRole('button', { name: 'Tải lại' }));
  await waitFor(() => expect(screen.queryByText('Private time row')).not.toBeInTheDocument());
  await act(async () => { pending.resolve({ items: [{ id: 'correction', at: entry.startAt, action: 'Update', before: entry }], nextCursor: null }); });
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); expect(screen.queryByText('Private time row')).not.toBeInTheDocument();
});
test('Time History grant revocation clears history even when entry read remains allowed', async () => {
  const user = userEvent.setup(); render(<TimeTrackingScreen onAuthLost={vi.fn()} />); await user.click(await screen.findByRole('button', { name: 'History' })); await screen.findByRole('dialog');
  vi.mocked(time.timeCapabilities).mockResolvedValue({ ...timeCaps, 'time.entry.history': false });
  const pending = deferred<time.TimePage>(); vi.mocked(time.timeEntries).mockReturnValue(pending.promise);
  fireEvent.click(screen.getByText('Tải lại'));
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument()); expect(screen.getByText('Private time row')).toBeInTheDocument();
  await act(async () => { pending.resolve({ items: [entry], nextCursor: null }); });
});
test('new Time history selection wins over an earlier response', async () => {
  vi.mocked(time.timeEntries).mockResolvedValue({ items: [entry, { ...entry, id: 'entry-b', description: 'Second private row' }], nextCursor: null });
  const pending = deferred<{ items: time.TimeCorrection[]; nextCursor: string | null }>(); vi.mocked(time.timeHistory).mockReturnValueOnce(pending.promise).mockResolvedValue({ items: [], nextCursor: null });
  const user = userEvent.setup(); render(<TimeTrackingScreen onAuthLost={vi.fn()} />); const buttons = await screen.findAllByRole('button', { name: 'History' }); await user.click(buttons[0]); await user.click(buttons[1]);
  expect(await screen.findByRole('dialog')).toHaveTextContent('Second private row'); await act(async () => { pending.resolve({ items: [], nextCursor: null }); }); expect(screen.getByRole('dialog')).toHaveTextContent('Second private row');
});
test('Focus preference Escape preserves changed input', async () => {
  const user = userEvent.setup(); render(<FocusScreen onAuthLost={vi.fn()} />); await user.click(await screen.findByRole('button', { name: 'Edit focus preferences' }));
  fireEvent.change(screen.getByLabelText('Focus minutes'), { target: { value: '30' } }); await user.keyboard('{Escape}');
  expect(screen.getByText('Bỏ thay đổi chưa lưu?')).toBeInTheDocument(); await user.click(screen.getByRole('button', { name: 'Tiếp tục sửa' })); expect(screen.getByLabelText('Focus minutes')).toHaveValue(30); expect(focus.focusSavePreferences).not.toHaveBeenCalled();
});
test('Focus read capability revocation clears open private preference draft', async () => {
  const user = userEvent.setup(); render(<FocusScreen onAuthLost={vi.fn()} />); await user.click(await screen.findByRole('button', { name: 'Edit focus preferences' }));
  vi.mocked(focus.focusCapabilities).mockResolvedValue({ ...focusCaps, 'focus.session.read': false }); fireEvent.click(screen.getByText('Tải lại')); await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
});
test('Focus preference action loss clears draft independently from session read', async () => {
  const user = userEvent.setup(); render(<FocusScreen onAuthLost={vi.fn()} />); await user.click(await screen.findByRole('button', { name: 'Edit focus preferences' }));
  vi.mocked(focus.focusCapabilities).mockResolvedValue({ ...focusCaps, 'focus.preference.update': false }); const pending = deferred<{ items: focus.FocusSession[]; nextCursor: string | null }>(); vi.mocked(focus.focusList).mockReturnValue(pending.promise); fireEvent.click(screen.getByText('Tải lại')); await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument()); expect(screen.getByRole('heading', { name: 'Active phase' })).toBeInTheDocument(); await act(async () => { pending.resolve({ items: [], nextCursor: null }); });
});
test('Focus does not start a second poll while the first is pending', async () => {
  vi.useFakeTimers();
  try {
    render(<FocusScreen onAuthLost={vi.fn()} />); await act(async () => { await vi.advanceTimersByTimeAsync(0); });
    const initial = vi.mocked(focus.focusList).mock.calls.length;
    const pending = deferred<{ items: focus.FocusSession[]; nextCursor: string | null }>(); vi.mocked(focus.focusList).mockReturnValue(pending.promise);
    await act(async () => { await vi.advanceTimersByTimeAsync(15000); });
    expect(focus.focusList).toHaveBeenCalledTimes(initial + 1);
    await act(async () => { pending.resolve({ items: [], nextCursor: null }); });
  } finally { vi.useRealTimers(); }
});
