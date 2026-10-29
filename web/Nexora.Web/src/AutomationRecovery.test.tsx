import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { AutomationScreen } from './AutomationScreen';
import { AutomationExecutionDialog } from './AutomationExecutionDialog';
import { AutomationLifecycleDialog } from './AutomationLifecycleDialog';
import { AutomationScheduleDialog } from './AutomationScheduleDialog';
import * as api from './automationApi';

vi.mock('./automationApi');
vi.mock('./AutomationProjectPicker', () => ({ AutomationProjectPicker: () => <p>Existing project participant</p> }));
const id = '319c21bb-4308-4d46-9d61-2c5c462e767b';
const etag = '"AAAAAAAAAAA="';
const definition: api.AutomationDefinition = { id, title: 'Existing local workflow', currentVersion: 1, state: 'Draft', enabled: false, updatedAt: '2026-10-25T00:00:00Z', etag, lifecycle: 'Active', deletionBatchId: null };
const shape: api.AutomationShape = { schemaVersion: 1, trigger: { key: 'manual', version: 1 }, steps: [{ id: 1, action: { key: 'tasks.task.create', version: 1 }, inputs: { projectId: '58a2b56f-9ee1-492e-9bf5-58ff6e570711', title: 'Local Task', description: null, startAt: '2028-01-03T10:00:00.0000000Z', endAt: '2028-01-04T10:00:00.0000000Z', priority: 'P2' } }] };
const version: api.AutomationVersion = { definitionId: id, versionNumber: 1, title: definition.title, definitionJson: JSON.stringify(shape), createdAt: definition.updatedAt };
const ack = { id, etag, versionNumber: 1 };
const timeout = () => Object.assign(new Error('The server may already have committed this command'), { status: 408, code: 'Timeout' });
const schedule: api.AutomationScheduleSnapshot = { definitionId: id, definitionVersion: 1, scheduleVersion: 1, etag, configuration: { frequency: 'Daily', timeZoneId: 'UTC', hour: 9, minute: 0, weekdays: 127, startAt: '2028-01-01T00:00:00Z', endAt: null, missedPolicy: 'Skip', enabled: false }, nextRuns: [], bindingStale: false, executionAvailable: false, recentOccurrences: [] };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.automationGet).mockResolvedValue({ definition, version });
  vi.mocked(api.automationCapabilities).mockResolvedValue(Object.fromEntries(['read', 'save', 'validate', 'history'].map(action => ['automation.definition.' + action, true])));
  vi.mocked(api.automationList).mockResolvedValue({ items: [definition], nextCursor: null });
  vi.mocked(api.automationScheduleGet).mockResolvedValue(schedule);
});

it('retains the exact existing definition save on HTTP408 and freezes the submitted draft', async () => {
  const save = vi.mocked(api.automationSave).mockRejectedValueOnce(timeout()).mockResolvedValue(ack);
  const user = userEvent.setup(); render(<AutomationScreen onAuthLost={vi.fn()} />);
  await user.click(await screen.findByRole('button', { name: 'Mở cấu hình' }));
  await user.type(screen.getByLabelText('Tên workflow'), ' retained edit');
  await user.click(screen.getByRole('button', { name: 'Lưu version' }));
  const retry = await screen.findByRole('button', { name: 'Retry cùng command' });
  expect(screen.getByLabelText('Tên workflow')).toBeDisabled();
  expect(screen.getByLabelText('Tên workflow')).toHaveValue('Existing local workflow retained edit');
  await user.click(retry); await waitFor(() => expect(save).toHaveBeenCalledTimes(2));
  expect(save.mock.calls[1]).toEqual(save.mock.calls[0]);
  expect(save.mock.calls[0][0]).toBe(id); expect(save.mock.calls[0][1]).toBe(etag); expect(save.mock.calls[0][3]).toMatch(/^[0-9a-f-]{36}$/i);
});

it('retains the original enable source, ETag and key on HTTP408', async () => {
  const enable = vi.mocked(api.automationEnable).mockRejectedValueOnce(timeout()).mockResolvedValue(ack);
  const user = userEvent.setup(); render(<AutomationExecutionDialog item={definition} operation="enable" onClose={vi.fn()} onDenied={() => false} />);
  await user.click(screen.getByRole('button', { name: 'Xác nhận' }));
  await user.click(await screen.findByRole('button', { name: 'Retry cùng command' }));
  await waitFor(() => expect(enable).toHaveBeenCalledTimes(2)); expect(enable.mock.calls[1]).toEqual(enable.mock.calls[0]);
  expect(enable.mock.calls[0].slice(0, 3)).toEqual([id, etag, true]); expect(enable.mock.calls[0][3]).toMatch(/^[0-9a-f-]{36}$/i);
});

it('retains the original Trash cohort and command key on HTTP408', async () => {
  const lifecycle = vi.mocked(api.automationLifecycle).mockRejectedValueOnce(timeout()).mockResolvedValue(ack);
  const user = userEvent.setup(); render(<AutomationLifecycleDialog item={definition} onClose={vi.fn()} onDenied={() => false} />);
  await user.click(screen.getByRole('button', { name: 'Chuyển vào Trash' }));
  await user.click(await screen.findByRole('button', { name: 'Retry cùng command' }));
  await waitFor(() => expect(lifecycle).toHaveBeenCalledTimes(2)); expect(lifecycle.mock.calls[1]).toEqual(lifecycle.mock.calls[0]);
  expect(lifecycle.mock.calls[0].slice(0, 3)).toEqual([id, etag, null]); expect(lifecycle.mock.calls[0][3]).toMatch(/^[0-9a-f-]{36}$/i);
});

it('retains the exact schedule body, ETag and key on HTTP408', async () => {
  const save = vi.mocked(api.automationScheduleSave).mockRejectedValueOnce(timeout()).mockResolvedValue(ack);
  const user = userEvent.setup(); render(<AutomationScheduleDialog id={id} onClose={vi.fn()} onDenied={() => false} />);
  const zone = await screen.findByLabelText('IANA timezone'); await waitFor(() => expect(zone).toBeEnabled());
  await user.clear(zone); await user.type(zone, 'Asia/Ho_Chi_Minh');
  await user.click(screen.getByRole('button', { name: 'Save schedule' }));
  const retry = await screen.findByRole('button', { name: 'Retry cùng command' });
  expect(zone).toBeDisabled(); await user.click(retry);
  await waitFor(() => expect(save).toHaveBeenCalledTimes(2)); expect(save.mock.calls[1]).toEqual(save.mock.calls[0]);
  expect(save.mock.calls[0][0]).toBe(id); expect(save.mock.calls[0][1]).toBe(etag); expect(save.mock.calls[0][2].timeZoneId).toBe('Asia/Ho_Chi_Minh'); expect(save.mock.calls[0][3]).toMatch(/^[0-9a-f-]{36}$/i);
});
