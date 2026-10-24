import { render, screen, within, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
vi.mock('./LearningPlanApi', async importOriginal => {
  const actual = await importOriginal<typeof import('./LearningPlanApi')>();
  return { ...actual, learningPlanCapabilities: vi.fn(), learningPlans: vi.fn(), learningPlan: vi.fn(), sendLearningPlan: vi.fn() };
});
import { NexoraApiError } from './api';
import { learningPlanCapabilities, learningPlans, learningPlan, sendLearningPlan } from './LearningPlanApi';
import { LearningPlanScreen } from './LearningPlanScreen';
const caps = { 'learning.plan.read': true, 'learning.plan.create': true };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(learningPlanCapabilities).mockResolvedValue(caps);
  vi.mocked(learningPlans).mockResolvedValue({ items: [], nextCursor: null });
  vi.mocked(learningPlan).mockResolvedValue({ plan: { id: 'plan', title: 'Saved plan', status: 'Planned', etag: '"AAAAAAAAAAA="', updatedAt: '', startsOn: null, endsOn: null, notes: null }, items: [], known: 0, unavailable: 0, progressLabel: 'No items' });
});
async function uncertainCreate() {
  const user = userEvent.setup(); render(<LearningPlanScreen ownerContext="owner-A:session-A" />);
  await user.click(await screen.findByRole('button', { name: 'Tạo kế hoạch' }));
  const dialog = screen.getByRole('dialog');
  await user.type(within(dialog).getByRole('textbox', { name: 'Tên' }), 'Private draft canary');
  await user.click(within(dialog).getByRole('button', { name: 'Lưu kế hoạch' }));
  await waitFor(() => expect(sendLearningPlan).toHaveBeenCalledTimes(1));
  return { user, dialog };
}
describe('Learning Plan uncertain command recovery', () => {
  it('keeps committed ACK final after refresh failure and clears a later definitive rejection', async () => {
    vi.mocked(learningPlanCapabilities).mockResolvedValueOnce(caps).mockRejectedValueOnce(new Error('refresh unavailable'));
    vi.mocked(sendLearningPlan).mockResolvedValueOnce({ id: 'plan', committed: true, detailAvailable: false })
      .mockRejectedValueOnce(new NexoraApiError('Invalid metadata', 422, 'ValidationFailed'));
    const { user } = await uncertainCreate();
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    await user.click(screen.getByRole('button', { name: 'Tạo kế hoạch' }));
    const dialog = screen.getByRole('dialog');
    await user.type(within(dialog).getByRole('textbox', { name: 'Tên' }), 'Correctable draft');
    await user.click(within(dialog).getByRole('button', { name: 'Lưu kế hoạch' }));
    await waitFor(() => expect(sendLearningPlan).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(within(dialog).getByRole('button', { name: 'Lưu kế hoạch' })).toBeEnabled());
    expect(within(dialog).queryByRole('button', { name: 'Gửi lại yêu cầu gốc' })).not.toBeInTheDocument();
    expect(within(dialog).getByDisplayValue('Correctable draft')).toBeInTheDocument();
    expect(within(dialog).getByRole('alert')).toHaveTextContent('Invalid metadata');
  });

  it('keeps retry reachable inside the modal and resends the exact frozen intent', async () => {
    vi.mocked(sendLearningPlan).mockRejectedValueOnce(new Error('response lost')).mockResolvedValue({ id: 'plan', committed: true, detailAvailable: false });
    const { user, dialog } = await uncertainCreate();
    const retry = await within(dialog).findByRole('button', { name: 'Gửi lại yêu cầu gốc' });
    expect(retry).toBeEnabled(); await user.click(retry);
    await screen.findByRole('heading', { name: 'Saved plan' });
    expect(vi.mocked(sendLearningPlan).mock.calls[0][0]).toBe(vi.mocked(sendLearningPlan).mock.calls[1][0]);
  });
  it('preserves unresolved key after later denial and removes private editor text', async () => {
    vi.mocked(sendLearningPlan).mockRejectedValueOnce(new Error('response lost')).mockRejectedValue(new NexoraApiError('Denied', 403, 'ModuleUnavailable'));
    const { user, dialog } = await uncertainCreate();
    await user.click(await within(dialog).findByRole('button', { name: 'Gửi lại yêu cầu gốc' }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    expect(screen.queryByDisplayValue('Private draft canary')).not.toBeInTheDocument();
    const retry = screen.getByRole('button', { name: 'Gửi lại yêu cầu gốc' }); expect(retry).toBeEnabled();
    await user.click(retry);
    expect(vi.mocked(sendLearningPlan).mock.calls[0][0]).toBe(vi.mocked(sendLearningPlan).mock.calls[2][0]);
  });
});
