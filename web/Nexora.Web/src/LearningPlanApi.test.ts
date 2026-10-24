import { describe, expect, it, vi } from 'vitest';
vi.mock('./api', () => ({ apiFetch: vi.fn() }));
import { apiFetch } from './api';
import { learningPlanIntent, sendLearningPlan, learningPlanSources, learningPlans } from './LearningPlanApi';
describe('Learning Plan intent contract', () => {
  it('freezes original key, body and revision for uncertain retry', async () => {
    const body = { source: { kind: 'Task', id: 'source' }, targetOn: '2026-10-21' };
    const intent = learningPlanIntent('/plan/items', 'POST', 'original-key', '"rowversion"', body);
    body.source.id = 'replacement';
    vi.mocked(apiFetch).mockRejectedValueOnce(new Error('lost response')).mockResolvedValue({ id: 'plan', committed: true, detailAvailable: false });
    await expect(sendLearningPlan(intent)).rejects.toThrow('lost response');
    await expect(sendLearningPlan(intent)).resolves.toMatchObject({ committed: true });
    const calls = vi.mocked(apiFetch).mock.calls.slice(-2);
    expect(calls[0]).toEqual(calls[1]); expect(Object.isFrozen(intent)).toBe(true);
    expect(JSON.parse(calls[1][1]!.body as string).source.id).toBe('source');
    expect(calls[1][1]!.headers).toEqual({ 'Idempotency-Key': 'original-key', 'If-Match': '"rowversion"' });
  });
  it('does not interpret malformed acknowledgement as committed', async () => {
    vi.mocked(apiFetch).mockResolvedValue({ id: 'plan', committed: false });
    await expect(sendLearningPlan(learningPlanIntent('', 'POST', 'key', null, { title: 'Plan' }))).rejects.toThrow('Unconfirmed');
  });
  it('encodes opaque filter-bound cursors and typed picker kind', async () => {
    vi.mocked(apiFetch).mockResolvedValue({ items: [], nextCursor: null });
    await learningPlanSources('Goal', 'opaque&cursor');
    expect(vi.mocked(apiFetch).mock.lastCall?.[0]).toBe('/api/v1/learning/plans/sources?kind=Goal&cursor=opaque%26cursor');
    await learningPlans('Planned', 'opaque&cursor');
    expect(vi.mocked(apiFetch).mock.lastCall?.[0]).toBe('/api/v1/learning/plans?status=Planned&cursor=opaque%26cursor');
  });
});
