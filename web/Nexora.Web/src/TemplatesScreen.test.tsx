import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { NexoraApiError } from './api';
import { TemplatesScreen } from './TemplatesScreen';
import * as api from './templateApi';
vi.mock('./templateApi');
const caps = Object.fromEntries(['read', 'create', 'update', 'archive', 'unarchive', 'trash', 'restore', 'purge', 'instantiate'].map(a => ['organization.template.' + a, true]));
const id = '733598bd-74c6-4bda-8cb1-eb4a38494e43';
const item: api.TemplateSummary = { id, name: 'Reading starter', moduleCode: 'FX21', resourceType: 'Bookmark', schemaVersion: 1, status: 'Active', etag: '"AAAAAAAAAAE="' };
const detail: api.TemplateDetail = { template: item, seed: { title: 'Read next', url: 'https://example.invalid/reading', description: 'Private seed description' }, canUse: true, unavailableReason: null };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.templateCapabilities).mockResolvedValue(caps);
  vi.mocked(api.listTemplates).mockResolvedValue({ items: [item], hasMore: false, page: 1 });
  vi.mocked(api.getTemplate).mockResolvedValue(detail);
  vi.mocked(api.sendTemplateIntent).mockResolvedValue(id);
  vi.mocked(api.prepareTemplateDraft).mockResolvedValue({ templateId: id, templateETag: item.etag, seed: detail.seed, moduleCode: 'FX21', resourceType: 'Bookmark' });
});
describe('Template library ownership and normal-form handoff', () => {
  it('distinguishes a denied library from an authorized empty result', async () => {
    vi.mocked(api.templateCapabilities).mockResolvedValue({});
    render(<TemplatesScreen onAuthLost={vi.fn()} />);
    await screen.findByText('Thư viện mẫu chưa khả dụng với quyền hiện tại.');
    expect(api.listTemplates).not.toHaveBeenCalled();
    expect(screen.queryByText('Một khởi đầu gọn gàng')).toBeNull();
  });
  it('opens a normal Bookmark draft after current server checks without creating a target', async () => {
    const user = userEvent.setup(), use = vi.fn();
    render(<TemplatesScreen onAuthLost={vi.fn()} onUseBookmarkTemplate={use} />);
    await user.click(await screen.findByText('Xem trước'));
    await screen.findByText('Private seed description');
    await user.click(screen.getByText('Dùng mẫu này'));
    await waitFor(() => expect(use).toHaveBeenCalledWith(detail.seed));
    expect(api.prepareTemplateDraft).toHaveBeenCalledWith(item);
    expect(api.sendTemplateIntent).not.toHaveBeenCalled();
  });
  it('keeps an archived template editable and usable outside Trash', async () => {
    const archived = { ...item, status: 'Archived' as const };
    vi.mocked(api.listTemplates).mockResolvedValue({ items: [archived], hasMore: false, page: 1 });
    vi.mocked(api.getTemplate).mockResolvedValue({ ...detail, template: archived });
    const user = userEvent.setup(); render(<TemplatesScreen onAuthLost={vi.fn()} onUseBookmarkTemplate={vi.fn()} />);
    await user.click(await screen.findByText('Sửa mẫu'));
    expect(screen.getByText('Lưu mẫu')).toBeEnabled();
  });
  it.each([0, 408, 500, 502, 503, 504])('freezes and retries exactly the same create request on unknown result %s', async status => {
    vi.mocked(api.sendTemplateIntent).mockRejectedValueOnce(new NexoraApiError('Unknown outcome', status)).mockResolvedValueOnce(id);
    const user = userEvent.setup(); render(<TemplatesScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Tạo mẫu mới'));
    await user.type(screen.getByLabelText('Tên mẫu'), 'My new starter');
    await user.click(screen.getByText('Lưu mẫu'));
    await screen.findByText('Unknown outcome');
    expect(screen.getByLabelText('Tên mẫu')).toBeDisabled();
    expect(screen.getByLabelText('Tiêu đề Bookmark')).toBeDisabled();
    await user.click(screen.getByText('Thử lại cùng yêu cầu'));
    await waitFor(() => expect(api.sendTemplateIntent).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.sendTemplateIntent).mock.calls[1]).toEqual(vi.mocked(api.sendTemplateIntent).mock.calls[0]);
    await waitFor(() => expect(screen.queryByLabelText('Tên mẫu')).toBeNull());
  });
  it('keeps dirty input when Escape discard is cancelled', async () => {
    const user = userEvent.setup(); render(<TemplatesScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Tạo mẫu mới')); await user.type(screen.getByLabelText('Tên mẫu'), 'Keep this draft');
    await user.keyboard('{Escape}'); await user.click(await screen.findByText('Tiếp tục sửa'));
    expect(screen.getByLabelText('Tên mẫu')).toHaveValue('Keep this draft');
    expect(api.sendTemplateIntent).not.toHaveBeenCalled();
  });
  it('clears private preview and editor when current authority is revoked', async () => {
    const user = userEvent.setup(); render(<TemplatesScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Xem trước')); await screen.findByText('Private seed description');
    vi.mocked(api.templateCapabilities).mockResolvedValue({});
    await act(async () => { window.dispatchEvent(new Event('focus')); });
    await waitFor(() => expect(screen.queryByText('Private seed description')).toBeNull());
    expect(screen.queryByText('Reading starter')).toBeNull();
  });
  it('stale version preserves the draft and requires comparison before a fresh Save', async () => {
    const user = userEvent.setup(); render(<TemplatesScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Sửa mẫu')); await user.clear(screen.getByLabelText('Tên mẫu')); await user.type(screen.getByLabelText('Tên mẫu'), 'Retained edit');
    vi.mocked(api.sendTemplateIntent).mockRejectedValueOnce(new NexoraApiError('Revision changed', 412));
    await user.click(screen.getByText('Lưu mẫu')); await screen.findByText('Revision changed');
    expect(screen.getByText('Lưu mẫu')).toBeDisabled();
    vi.mocked(api.getTemplate).mockResolvedValue({ ...detail, template: { ...item, name: 'Current server name', etag: '"AAAAAAAAAAI="' } });
    await user.click(screen.getByText('Tải bản hiện tại để so sánh'));
    await screen.findByText('Current server name');
    expect(screen.getByLabelText('Tên mẫu')).toHaveValue('Retained edit');
    expect(api.sendTemplateIntent).toHaveBeenCalledTimes(1);
    await user.click(screen.getByText('Lưu mẫu'));
    await waitFor(() => expect(api.sendTemplateIntent).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.sendTemplateIntent).mock.calls[1][0].etag).toBe('"AAAAAAAAAAI="');
  });
});
