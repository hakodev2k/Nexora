import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ShoppingRecordsScreen } from './ShoppingRecordsScreen';
import * as api from './shoppingRecordsApi';

vi.mock('./shoppingRecordsApi', () => ({ shoppingRecordCapabilities: vi.fn(), shoppingRecordList: vi.fn(), shoppingRecordGet: vi.fn(), sendShoppingIntent: vi.fn() }));
const id = '23ef11a6-a65d-4d78-a15d-5df95ba7ba26';
const seller: api.SellerRecord = { id, title: 'Synthetic public shop', url: 'https://example.invalid/shop', linkedOrderCount: null, etag: '"AAAAAAAAAAE="' };
const caps = Object.fromEntries(['seller', 'order', 'warranty'].flatMap(kind => ['read', 'create', 'update'].map(action => [`shopping.${kind}.${action}`, true])));
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.shoppingRecordCapabilities).mockResolvedValue(caps);
  vi.mocked(api.shoppingRecordList).mockResolvedValue({ items: [seller], page: 1, hasMore: false });
  vi.mocked(api.shoppingRecordGet).mockResolvedValue(seller);
  vi.mocked(api.sendShoppingIntent).mockResolvedValue({ id });
});
describe('Shopping manual records', () => {
  it('renders a real permitted record and opens detail without rendering an external-open link', async () => {
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="sellers" onAuthLost={vi.fn()} />);
    await screen.findByText(seller.title); expect(screen.queryByRole('link')).toBeNull();
    await user.click(screen.getByText('Xem chi tiết')); await screen.findByText('Chỉnh sửa');
    expect(api.shoppingRecordGet).toHaveBeenCalledWith('sellers', id);
  });
  it('distinguishes denied read from an authorized empty list and does not fetch unauthorized records', async () => {
    vi.mocked(api.shoppingRecordCapabilities).mockResolvedValue({ ...caps, 'shopping.seller.read': false, 'shopping.seller.create': false });
    render(<ShoppingRecordsScreen kind="sellers" onAuthLost={vi.fn()} />);
    await screen.findByText('Chưa có quyền xem mục này trong phiên hiện tại.');
    expect(api.shoppingRecordList).not.toHaveBeenCalled(); expect(screen.queryByText('Bắt đầu từ một bản ghi nhỏ')).toBeNull();
  });
  it.each([408, 500, 502, 503, 504])('freezes an uncertain create and retries its exact immutable command after %s', async status => {
    vi.mocked(api.sendShoppingIntent).mockRejectedValueOnce(Object.assign(new Error('Save outcome unknown'), { status })).mockResolvedValueOnce({ id });
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="sellers" onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Thêm người bán')); await user.type(screen.getByLabelText('Tên người bán'), 'Retained shop');
    await user.click(screen.getByRole('button', { name: /^Lưu$/ }));
    await screen.findByText('Save outcome unknown'); expect(screen.getByLabelText('Tên người bán')).toBeDisabled();
    await user.click(screen.getByText('Thử lại đúng yêu cầu lưu'));
    await waitFor(() => expect(api.sendShoppingIntent).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.sendShoppingIntent).mock.calls[1][0]).toEqual(vi.mocked(api.sendShoppingIntent).mock.calls[0][0]);
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
  });
  it('clears private rows and editor after a current write denial', async () => {
    vi.mocked(api.sendShoppingIntent).mockRejectedValue(Object.assign(new Error('Current action denied'), { status: 403 }));
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="sellers" onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Thêm người bán')); await user.type(screen.getByLabelText('Tên người bán'), 'Private draft');
    await user.click(screen.getByRole('button', { name: /^Lưu$/ })); await screen.findByText('Current action denied');
    expect(screen.queryByText(seller.title)).toBeNull(); expect(screen.queryByRole('dialog')).toBeNull();
  });
  it('keeps a rejected revision locked until the owner explicitly resolves it', async () => {
    vi.mocked(api.sendShoppingIntent).mockRejectedValue(Object.assign(new Error('Revision changed'), { status: 412 }));
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="sellers" initialRecordId={id} onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Chỉnh sửa')); await user.clear(screen.getByLabelText('Tên người bán')); await user.type(screen.getByLabelText('Tên người bán'), 'Retained local draft');
    await user.click(screen.getByRole('button', { name: /^Lưu$/ })); await screen.findByText('Phiên bản hiện tại');
    expect(screen.getByLabelText('Tên người bán')).toBeDisabled(); expect(screen.getByLabelText('Tên người bán')).toHaveValue('Retained local draft');
    await user.click(screen.getByText('Tải lại nội dung hiện tại'));
    await waitFor(() => expect(screen.getByLabelText('Tên người bán')).toBeEnabled()); expect(screen.getByLabelText('Tên người bán')).toHaveValue(seller.title);
  });
  it('provides a safe way back from an unavailable deep-linked record', async () => {
    vi.mocked(api.shoppingRecordGet).mockRejectedValue(Object.assign(new Error('Record unavailable'), { status: 404 }));
    const user = userEvent.setup(); const select = vi.fn(); render(<ShoppingRecordsScreen kind="sellers" initialRecordId={id} onAuthLost={vi.fn()} onSelectRecord={select} />);
    await user.click(await screen.findByText('Về danh sách')); expect(select).toHaveBeenCalledWith();
    await screen.findByText(seller.title);
  });
  it('requires a product from the newly chosen order instead of silently keeping the previous order line', async () => {
    const orderId = '23ef11a6-a65d-4d78-a15d-5df95ba7ba27', oldLineId = '23ef11a6-a65d-4d78-a15d-5df95ba7ba28', newLineId = '23ef11a6-a65d-4d78-a15d-5df95ba7ba29';
    const warranty: api.WarrantyRecord = { id, orderLineId: oldLineId, title: 'Existing coverage', coverageKind: 'Unknown', startsOn: null, endsOn: null, provider: null, terms: null, claimContact: null, etag: seller.etag };
    const order: api.ShoppingOrder = { id: orderId, sellerId: null, sellerTitle: null, orderNumber: 'NEW-ORDER', purchasedOn: '2026-10-25', currency: 'VND', shippingAmount: '0', taxAmount: '0', discountAmount: '0', totalAmount: '100', notes: null, status: 'Draft', etag: seller.etag,
      lines: [{ id: newLineId, title: 'New order product', quantity: '1', unitAmount: '100', variant: null }] };
    let release!: (row: api.ShoppingOrder) => void;
    const pendingOrder = new Promise<api.ShoppingOrder>(resolve => { release = resolve; });
    vi.mocked(api.shoppingRecordGet).mockImplementation(async kind => kind === 'orders' ? pendingOrder : warranty);
    vi.mocked(api.shoppingRecordList).mockImplementation(async kind => ({ items: kind === 'orders' ? [order] : [warranty], page: 1, hasMore: false }));
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="warranties" initialRecordId={id} onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Chỉnh sửa')); expect(screen.getByLabelText('Sản phẩm được bảo hành')).toHaveValue(oldLineId);
    await user.click(screen.getByRole('button', { name: 'Tìm để chọn' }));
    await user.selectOptions(screen.getByLabelText('Chọn đơn mua sắm'), orderId);
    fireEvent.submit(screen.getByRole('dialog').querySelector('form')!);
    expect(api.sendShoppingIntent).not.toHaveBeenCalled();
    release(order);
    await screen.findByRole('option', { name: 'New order product' });
    expect(screen.getByLabelText('Sản phẩm được bảo hành')).toHaveValue('');
    await user.click(screen.getByRole('button', { name: /^Lưu$/ })); expect(api.sendShoppingIntent).not.toHaveBeenCalled();
    await user.selectOptions(screen.getByLabelText('Sản phẩm được bảo hành'), newLineId);
    await user.click(screen.getByRole('button', { name: /^Lưu$/ }));
    await waitFor(() => expect(api.sendShoppingIntent).toHaveBeenCalledTimes(1));
    expect(JSON.parse(vi.mocked(api.sendShoppingIntent).mock.calls[0][0].body).orderLineId).toBe(newLineId);
  });
  it('does not append a later page for an edited source query before searching that query from page one', async () => {
    vi.mocked(api.shoppingRecordList).mockImplementation(async kind => ({ items: kind === 'sellers' ? [seller] : [], page: 1, hasMore: kind === 'sellers' }));
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="orders" onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Thêm đơn mua sắm'));
    await user.type(screen.getByLabelText('Tìm tên người bán'), 'Old query'); await user.click(screen.getByRole('button', { name: 'Tìm để chọn' }));
    await screen.findByRole('button', { name: 'Xem thêm kết quả' });
    await user.clear(screen.getByLabelText('Tìm tên người bán')); await user.type(screen.getByLabelText('Tìm tên người bán'), 'New query');
    expect(screen.queryByRole('button', { name: 'Xem thêm kết quả' })).toBeNull();
    await user.click(screen.getByRole('button', { name: 'Tìm để chọn' }));
    await waitFor(() => expect(api.shoppingRecordList).toHaveBeenCalledWith('sellers', 1, 'New query'));
    expect(vi.mocked(api.shoppingRecordList).mock.calls.some(call => call[1] === 2 && call[2] === 'New query')).toBe(false);
  });
  it('cannot silently save the old order line after a failed order switch and recovers after explicitly choosing again', async () => {
    const orderId = '23ef11a6-a65d-4d78-a15d-5df95ba7ba27', oldLineId = '23ef11a6-a65d-4d78-a15d-5df95ba7ba28', newLineId = '23ef11a6-a65d-4d78-a15d-5df95ba7ba29';
    const warranty: api.WarrantyRecord = { id, orderLineId: oldLineId, title: 'Retained warranty draft', coverageKind: 'Unknown', startsOn: null, endsOn: null, provider: null, terms: 'Preserve my terms', claimContact: null, etag: seller.etag };
    const order: api.ShoppingOrder = { id: orderId, sellerId: null, sellerTitle: null, orderNumber: 'RECOVERED-ORDER', purchasedOn: '2026-10-25', currency: 'VND', shippingAmount: '0', taxAmount: '0', discountAmount: '0', totalAmount: '100', notes: null, status: 'Draft', etag: seller.etag,
      lines: [{ id: newLineId, title: 'Recovered order product', quantity: '1', unitAmount: '100', variant: null }] };
    let failed = false;
    vi.mocked(api.shoppingRecordGet).mockImplementation(async kind => {
      if (kind !== 'orders') return warranty;
      if (!failed) { failed = true; throw Object.assign(new Error('Order unavailable temporarily'), { status: 503 }); }
      return order;
    });
    vi.mocked(api.shoppingRecordList).mockImplementation(async kind => ({ items: kind === 'orders' ? [order] : [warranty], page: 1, hasMore: false }));
    const user = userEvent.setup(); render(<ShoppingRecordsScreen kind="warranties" initialRecordId={id} onAuthLost={vi.fn()} />);
    await user.click(await screen.findByText('Chỉnh sửa')); await user.click(screen.getByRole('button', { name: 'Tìm để chọn' }));
    await user.selectOptions(screen.getByLabelText('Chọn đơn mua sắm'), orderId); await screen.findByText('Order unavailable temporarily');
    expect(screen.getByLabelText('Sản phẩm được bảo hành')).toHaveValue('');
    expect(screen.getByLabelText('Tên bảo hành')).toHaveValue('Retained warranty draft'); expect(screen.getByLabelText('Điều khoản')).toHaveValue('Preserve my terms');
    await user.click(screen.getByRole('button', { name: /^Lưu$/ })); expect(api.sendShoppingIntent).not.toHaveBeenCalled();
    await user.selectOptions(screen.getByLabelText('Chọn đơn mua sắm'), orderId); await screen.findByRole('option', { name: 'Recovered order product' });
    await user.selectOptions(screen.getByLabelText('Sản phẩm được bảo hành'), newLineId); await user.click(screen.getByRole('button', { name: /^Lưu$/ }));
    await waitFor(() => expect(api.sendShoppingIntent).toHaveBeenCalledTimes(1));
    expect(JSON.parse(vi.mocked(api.sendShoppingIntent).mock.calls[0][0].body).orderLineId).toBe(newLineId);
  });
});
