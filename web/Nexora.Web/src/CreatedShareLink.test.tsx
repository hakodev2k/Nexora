import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { apiFetch } from './api';
import { CreatedShareLink } from './CreatedShareLink';

vi.mock('./api', () => ({ apiFetch: vi.fn() }));
const writeText = vi.fn();
const props = { id: '11111111-1111-1111-1111-111111111111', token: 'synthetic-memory-only', onUnavailable: vi.fn() };
beforeEach(() => {
  vi.mocked(apiFetch).mockReset(); writeText.mockReset(); props.onUnavailable.mockReset();
  Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText } });
});
it('checks current authority before copying a complete same-origin URL', async () => {
  vi.mocked(apiFetch).mockResolvedValue({ allowed: true }); writeText.mockResolvedValue(undefined);
  render(<CreatedShareLink {...props} />); fireEvent.click(screen.getByRole('button', { name: 'Copy URL' }));
  await screen.findByText('Đã copy URL.');
  expect(apiFetch).toHaveBeenCalledWith('/api/v1/sharing/links/' + props.id + '/copy-capability');
  expect(writeText).toHaveBeenCalledWith(window.location.origin + '/share/' + props.token);
});
it.each([401, 403, 404])('clears the creation result when authority is unavailable (%s)', async status => {
  vi.mocked(apiFetch).mockRejectedValue(Object.assign(new Error('Unavailable'), { status }));
  render(<CreatedShareLink {...props} />); fireEvent.click(screen.getByRole('button', { name: 'Copy URL' }));
  await waitFor(() => expect(props.onUnavailable).toHaveBeenCalledOnce()); expect(writeText).not.toHaveBeenCalled();
});
it('does not copy after the creation result has unmounted while authority is pending', async () => {
  let resolve!: (value: unknown) => void;
  vi.mocked(apiFetch).mockImplementation(() => new Promise<any>(done => { resolve = done; }));
  const view = render(<CreatedShareLink {...props} />); fireEvent.click(screen.getByRole('button', { name: 'Copy URL' })); view.unmount();
  await act(async () => { resolve({ allowed: true }); }); expect(writeText).not.toHaveBeenCalled();
});
it('reports clipboard rejection without showing a success or exposing the browser error', async () => {
  vi.mocked(apiFetch).mockResolvedValue({ allowed: true }); writeText.mockRejectedValue(new Error('Private clipboard diagnostic'));
  render(<CreatedShareLink {...props} />); fireEvent.click(screen.getByRole('button', { name: 'Copy URL' }));
  await screen.findByText('Không thể copy URL. Kiểm tra quyền clipboard rồi thử lại.');
  expect(screen.queryByText('Đã copy URL.')).not.toBeInTheDocument(); expect(screen.queryByText('Private clipboard diagnostic')).not.toBeInTheDocument();
});
