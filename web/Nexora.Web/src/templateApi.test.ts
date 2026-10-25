import { beforeEach, expect, it, vi } from 'vitest';
import { apiFetch } from './api';
import { prepareTemplateDraft, sendTemplateIntent, type TemplateSummary } from './templateApi';
vi.mock('./api', async original => ({ ...await original<typeof import('./api')>(), apiFetch: vi.fn() }));
const id = '733598bd-74c6-4bda-8cb1-eb4a38494e43';
const item: TemplateSummary = { id, name: 'Template', moduleCode: 'FX21', resourceType: 'Bookmark', schemaVersion: 1, status: 'Active', etag: '"AAAAAAAAAAE="' };
beforeEach(() => vi.resetAllMocks());
it('validates the prepared target/provider and exact template revision before handoff', async () => {
  const value = { templateId: id, templateETag: item.etag, moduleCode: 'FX21', resourceType: 'Bookmark', seed: { title: 'Title', url: '', description: null } };
  vi.mocked(apiFetch).mockResolvedValue(value);
  expect(await prepareTemplateDraft(item)).toEqual(value);
  vi.mocked(apiFetch).mockResolvedValue({ ...value, moduleCode: 'FX28' });
  await expect(prepareTemplateDraft(item)).rejects.toMatchObject({ code: 'TemplateUnavailable' });
  vi.mocked(apiFetch).mockResolvedValue({ ...value, templateId: 'foreign-template' });
  await expect(prepareTemplateDraft(item)).rejects.toMatchObject({ code: 'TemplateUnavailable' });
  vi.mocked(apiFetch).mockResolvedValue({ ...value, templateETag: '"AAAAAAAAAAI="' });
  await expect(prepareTemplateDraft(item)).rejects.toMatchObject({ code: 'TemplateUnavailable' });
});
it('treats a mismatched mutation acknowledgement as unknown rather than accepting success', async () => {
  vi.mocked(apiFetch).mockResolvedValue({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', committed: true, detailAvailable: false });
  await expect(sendTemplateIntent({ id, action: 'update', key: 'existing-key', body: '{}', etag: item.etag })).rejects.toMatchObject({ status: 0, code: 'UnexpectedAcknowledgement' });
});
