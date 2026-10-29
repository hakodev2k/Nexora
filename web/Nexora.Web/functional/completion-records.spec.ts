import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';
import type { Browser, Locator, Page } from '@playwright/test';

// All positive business writes below use the actual feature forms/API/SQL. Only
// legitimate SuperAdmin preview/commit APIs prepare and restore module access.
async function enabled(browser: Browser, page: Page, code: 'FX31' | 'FX40') {
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await context.newPage(); await login(admin, 'SuperAdmin');
  const me = await api(page, 'getMe');
  const module = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === code);
  expect(module, 'Installed module exists').toBeTruthy();
  expect(module.state, 'Only already Ready modules may be enabled').toBe('Ready');
  const before = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === code);
  const policy = async (systemEnabled: boolean) => {
    const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === code);
    if (current.systemEnabled === systemEnabled) return;
    const change = { systemEnabled, registrationEnabled: systemEnabled ? module.registrationEnabled : false };
    const preview = await api(admin, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  const grant = async (on: boolean) => {
    const current = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === code);
    if (current.enabled === on) return;
    const changes = [{ moduleId: before.moduleId, enabled: on }];
    const preview = await api(admin, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  try { await policy(true); await grant(true); }
  catch (error) { try { await grant(before.enabled); await policy(module.systemEnabled); } finally { await context.close(); } throw error; }
  return { me, grant, close: async () => { try {
    await grant(before.enabled); await policy(module.systemEnabled);
    const restoredModule = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === code);
    expect(restoredModule.systemEnabled).toBe(module.systemEnabled); expect(restoredModule.registrationEnabled).toBe(module.registrationEnabled);
    const restoredGrant = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === code);
    expect(restoredGrant.enabled).toBe(before.enabled);
  } finally { await context.close(); } } };
}

test.beforeEach(async ({ page }, info) => {
  page.setDefaultTimeout(15000);
  if (info.project.name === 'desktop') await page.setViewportSize({ width: 1440, height: 900 });
});

async function capture(page: Page, name: string) {
  const size = page.viewportSize()!;
  const folder = join('artifacts', 'existing-records-review', `${size.width}x${size.height}`);
  mkdirSync(folder, { recursive: true });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: join(folder, `${name}.png`), fullPage: true });
  const modal = page.getByRole('dialog');
  if (await modal.count() === 1) {
    await modal.evaluate(element => { element.scrollTop = 0; });
    await expect(modal.getByRole('heading').first()).toBeInViewport();
    await page.screenshot({ path: join(folder, `${name}-viewport-top.png`), fullPage: false });
    const save = modal.getByRole('button', { name: /^(Lưu|Lưu chứng chỉ)$/ }).first();
    if (await save.count() && await save.isEnabled()) {
      await save.scrollIntoViewIfNeeded(); await expect(save).toBeInViewport(); await save.focus(); await expect(save).toBeFocused();
    }
    const cancel = modal.getByRole('button', { name: 'Hủy', exact: true });
    await expect(cancel).toBeEnabled(); await cancel.scrollIntoViewIfNeeded(); await expect(cancel).toBeInViewport(); await cancel.focus();
    // Exercise keyboard modality: programmatic focus after a pointer action does
    // not request :focus-visible in Chromium, while actual Tab navigation does.
    await page.keyboard.press('Shift+Tab'); await page.keyboard.press('Tab'); await expect(cancel).toBeFocused();
    expect(await cancel.evaluate(element => {
      const style = getComputedStyle(element);
      return (style.outlineStyle !== 'none' && Number.parseFloat(style.outlineWidth) > 0) || style.boxShadow !== 'none';
    }), 'Cancel has a rendered visible focus indicator').toBe(true);
    await page.screenshot({ path: join(folder, `${name}-viewport-actions.png`), fullPage: false });
    const before = await page.evaluate(() => ({ y: scrollY, max: document.documentElement.scrollHeight - innerHeight }));
    await page.mouse.move(1, Math.floor(size.height / 2)); await page.mouse.wheel(0, before.y > before.max / 2 ? -400 : 400);
    await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    const after = await page.evaluate(() => scrollY);
    expect(after, 'Background page stays fixed while the dialog is open').toBe(before.y);
    writeFileSync(join(folder, `${name}-viewport-proof.json`), JSON.stringify({ viewport: size, topAndActionsCaptured: true, keyboardCancelFocused: true, visibleFocusIndicator: true, before, after }, null, 2));
  }
}
async function save(page: Page, path: string, method: string, label = 'Lưu') {
  const pending = page.waitForResponse(r => new URL(r.url()).pathname === path && r.request().method() === method);
  await page.getByRole('dialog').getByRole('button', { name: label, exact: true }).click();
  const result = await pending; expect(result.ok()).toBe(true); const ack = await result.json();
  expect(ack.committed).toBe(true); expect(ack.detailAvailable).toBe(false);
  await expect(page.getByRole('dialog')).toHaveCount(0); return ack.id as string;
}
function owner(kind: string, id: string, ownerId: string) {
  const rows = sql(kind, id); expect(rows).toHaveLength(1); expect(rows[0].OwnerId.toLowerCase()).toBe(ownerId.toLowerCase()); return rows[0];
}
async function fillOrder(modal: Locator, reference: string, product: string, price: string) {
  await modal.getByLabel('Mã đơn (tùy chọn)', { exact: true }).fill(reference);
  await modal.getByLabel('Ngày mua', { exact: true }).fill('2026-10-25');
  await modal.getByLabel('Tiền tệ (ví dụ VND, USD)', { exact: true }).fill('VND');
  await modal.getByLabel('Tên sản phẩm', { exact: true }).fill(product);
  await modal.getByLabel('Số lượng', { exact: true }).fill('2');
  await modal.getByLabel('Đơn giá', { exact: true }).fill(price);
}

test('Existing records Shopping: Seller and Draft Order persistence, stale update, duplicate reference and Warranty order switch', async ({ page, browser }) => {
  test.setTimeout(240000); await page.goto('/'); const access = await enabled(browser, page, 'FX31');
  const token = unique('records'), root = '/api/v1/shopping/records';
  try {
    await page.goto('/shopping/sellers'); await page.getByRole('button', { name: 'Thêm người bán', exact: true }).click();
    let modal = page.getByRole('dialog'); await modal.getByLabel('Tên người bán', { exact: true }).fill(token + '-seller');
    await modal.getByLabel('Website công khai', { exact: true }).fill('https://example.invalid/shop');
    await capture(page, token + '-seller-form'); const sellerId = await save(page, root + '/sellers', 'POST');
    expect(owner('Seller', sellerId, access.me.personalSpaceId).Title).toBe(token + '-seller');
    await page.reload(); await page.getByRole('button', { name: 'Chỉnh sửa', exact: true }).click();
    await page.getByLabel('Tên người bán', { exact: true }).fill(token + '-seller-edited'); await save(page, root + '/sellers/' + sellerId, 'PUT');
    expect(sql('Seller', sellerId)[0].Title).toBe(token + '-seller-edited');
    await capture(page, token + '-seller-detail');

    await page.goto('/shopping/orders'); await page.getByRole('button', { name: 'Thêm đơn mua sắm', exact: true }).click(); modal = page.getByRole('dialog');
    await fillOrder(modal, token + '-order-A', token + '-product-A', '100.12345678');
    await modal.getByLabel('Phí vận chuyển', { exact: true }).fill('10'); await modal.getByLabel('Thuế', { exact: true }).fill('1'); await modal.getByLabel('Giảm giá', { exact: true }).fill('0.24691356');
    await modal.getByLabel('Tìm tên người bán', { exact: true }).fill(token + '-seller-edited'); await modal.getByRole('button', { name: 'Tìm để chọn', exact: true }).click();
    await modal.getByRole('combobox', { name: /^Người bán/ }).selectOption(sellerId); await capture(page, token + '-order-form');
    const orderId = await save(page, root + '/orders', 'POST');
    const orderCard = page.locator('.shopping-record-grid .resource-card').filter({ has: page.getByRole('heading', { name: token + '-order-A', exact: true }) });
    await expect(orderCard).toBeVisible();
    expect(await orderCard.locator('.shopping-amount').evaluate(element => {
      const range = document.createRange(); range.selectNodeContents(element);
      // React renders amount, space and currency as separate text nodes; count
      // rendered row positions, not multiple inline fragments on the same row.
      return new Set(Array.from(range.getClientRects(), rect => Math.round(rect.top))).size;
    }), 'A short exact currency total must remain readable on one line in its card').toBe(1);
    const order = (await response(page, root + '/orders/' + orderId, 'GET')).body;
    expect(order.totalAmount).toBe('211'); expect(order.sellerTitle).toBe(token + '-seller-edited'); expect(order.status).toBe('Draft');
    const persistedOrder = owner('PurchaseOrder', orderId, access.me.personalSpaceId);
    expect(persistedOrder.TotalAmount).toBe(211); expect(persistedOrder.SellerId.toLowerCase()).toBe(sellerId.toLowerCase());
    expect(order.lines[0].quantity).toBe('2'); expect(order.lines[0].unitAmount).toBe('100.12345678');
    owner('OrderLine', order.lines[0].id, access.me.personalSpaceId);
    await page.reload(); await page.getByRole('button', { name: 'Chỉnh sửa', exact: true }).click(); modal = page.getByRole('dialog'); await modal.getByRole('textbox', { name: 'Ghi chú', exact: true }).fill('Preserved UI order draft');
    const concurrent = { sellerId: order.sellerId, orderNumber: order.orderNumber, purchasedOn: order.purchasedOn, currency: order.currency,
      shippingAmount: order.shippingAmount, taxAmount: order.taxAmount, discountAmount: order.discountAmount, lines: order.lines,
      notes: 'Concurrent current order', confirmDuplicateReference: false };
    expect((await response(page, root + '/orders/' + orderId, 'PUT', concurrent, order.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); await expect(modal.getByRole('heading', { name: 'Phiên bản hiện tại', exact: true })).toBeVisible();
    await expect(modal.getByRole('textbox', { name: 'Ghi chú', exact: true })).toHaveValue('Preserved UI order draft'); await capture(page, token + '-order-conflict');
    await modal.getByRole('button', { name: 'Giữ bản nháp trên phiên bản mới', exact: true }).click(); await save(page, root + '/orders/' + orderId, 'PUT');
    expect(sql('PurchaseOrder', orderId)[0].Notes).toBe('Preserved UI order draft');

    await page.goto('/shopping/orders'); await page.getByRole('button', { name: 'Thêm đơn mua sắm', exact: true }).click(); modal = page.getByRole('dialog');
    await fillOrder(modal, token + '-order-A', token + '-product-B', '20');
    const duplicate = page.waitForResponse(r => new URL(r.url()).pathname === root + '/orders' && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); expect((await duplicate).status()).toBe(409);
    await modal.getByLabel('Tôi đã kiểm tra và muốn lưu thêm một đơn có mã này', { exact: true }).check();
    const secondOrderId = await save(page, root + '/orders', 'POST'); const second = (await response(page, root + '/orders/' + secondOrderId, 'GET')).body;
    expect(second.totalAmount).toBe('40');

    await page.goto('/shopping/warranties'); await page.getByRole('button', { name: 'Thêm bảo hành', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByLabel('Tên bảo hành', { exact: true }).fill(token + '-warranty');
    await modal.getByLabel('Tìm mã đơn hoặc người bán', { exact: true }).fill(token + '-order-A'); await modal.getByRole('button', { name: 'Tìm để chọn', exact: true }).click();
    await modal.getByRole('combobox', { name: /^Chọn đơn mua sắm/ }).selectOption(orderId);
    await modal.getByRole('combobox', { name: /^Sản phẩm được bảo hành/ }).selectOption(order.lines[0].id);
    await modal.getByRole('combobox', { name: /^Thời hạn/ }).selectOption('Fixed'); await modal.getByLabel('Ngày bắt đầu', { exact: true }).fill('2026-10-25'); await modal.getByLabel('Ngày kết thúc', { exact: true }).fill('2027-10-25');
    const warrantyId = await save(page, root + '/warranties', 'POST'); expect(owner('Warranty', warrantyId, access.me.personalSpaceId).OrderLineId.toLowerCase()).toBe(order.lines[0].id.toLowerCase());
    await page.reload(); await capture(page, token + '-warranty-detail'); await page.getByRole('button', { name: 'Chỉnh sửa', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByLabel('Tìm mã đơn hoặc người bán', { exact: true }).fill(token + '-order-A'); await modal.getByRole('button', { name: 'Tìm để chọn', exact: true }).click();
    await modal.getByRole('combobox', { name: /^Chọn đơn mua sắm/ }).selectOption(secondOrderId);
    await expect(modal.getByRole('combobox', { name: /^Sản phẩm được bảo hành/ })).toHaveValue('');
    await capture(page, token + '-warranty-order-switch');
    await modal.getByRole('combobox', { name: /^Sản phẩm được bảo hành/ }).selectOption(second.lines[0].id); await modal.getByLabel('Đơn vị bảo hành', { exact: true }).fill('Updated synthetic warranty');
    await save(page, root + '/warranties/' + warrantyId, 'PUT'); expect(sql('Warranty', warrantyId)[0].OrderLineId.toLowerCase()).toBe(second.lines[0].id.toLowerCase());
    await page.reload(); await expect(page.getByText('Updated synthetic warranty', { exact: true })).toBeVisible();
    const titleSize = await page.locator('.shopping-detail h2').evaluate(element => ({ height: element.getBoundingClientRect().height, line: Number.parseFloat(getComputedStyle(element).lineHeight) }));
    expect(titleSize.height, 'Selected record title must not collapse to a letter-per-line column').toBeLessThanOrEqual(titleSize.line * 3);
    await capture(page, token + '-warranty-edited');
  } catch (error) { await capture(page, token + '-failed').catch(() => {}); throw error; }
  finally { await access.close(); }
});

test('Existing records Certification: normal create, renew, history and read-only Archive then Unarchive', async ({ page, browser }) => {
  test.setTimeout(180000); await page.goto('/'); const access = await enabled(browser, page, 'FX40'); const token = unique('certification');
  const root = '/api/v1/learning/certifications';
  try {
    await page.goto('/learning/certifications'); await page.getByRole('button', { name: 'Thêm chứng chỉ', exact: true }).click(); let modal = page.getByRole('dialog');
    await modal.getByLabel('Tên chứng chỉ', { exact: true }).fill(token); await modal.getByLabel('Đơn vị cấp', { exact: true }).fill('Synthetic learning issuer');
    await modal.getByLabel('Ngày cấp (tùy chọn)', { exact: true }).fill('2025-01-01'); await modal.getByLabel('Ngày hết hạn (tùy chọn)', { exact: true }).fill('2026-01-01');
    await capture(page, token + '-create-form'); const id = await save(page, root + '/', 'POST', 'Lưu chứng chỉ');
    owner('Certification', id, access.me.personalSpaceId);
    await page.goto('/learning/certifications/' + id); await expect(page.getByRole('heading', { name: token, exact: true })).toBeVisible(); await expect(page.getByText('Đã hết hạn', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Ghi nhận gia hạn', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByLabel('Ngày cấp (tùy chọn)', { exact: true }).fill('2026-10-25'); await modal.getByLabel('Ngày hết hạn (tùy chọn)', { exact: true }).fill('2028-10-25'); await modal.getByLabel('Ghi chú gia hạn (tùy chọn)', { exact: true }).fill('Synthetic renewal preserves prior dates');
    await save(page, root + '/' + id + '/renew', 'POST', 'Lưu chứng chỉ'); await page.reload();
    await expect(page.getByText('Còn hiệu lực', { exact: true })).toBeVisible(); await page.getByRole('button', { name: 'Xem lịch sử', exact: true }).click();
    const history = page.getByRole('region', { name: 'Lịch sử chứng chỉ', exact: true }); await expect(history.getByText('Synthetic renewal preserves prior dates', { exact: true })).toBeVisible();
    await expect(history).toContainText('01/01/2025'); await capture(page, token + '-renewal-history');
    await page.getByRole('button', { name: 'Lưu trữ', exact: true }).click(); await save(page, root + '/' + id + '/archive', 'POST', 'Lưu trữ');
    expect(sql('Certification', id)[0].Status).toBe('Archived'); await page.reload();
    await expect(page.getByRole('button', { name: 'Sửa thông tin', exact: true })).toHaveCount(0); await expect(page.getByRole('button', { name: 'Ghi nhận gia hạn', exact: true })).toHaveCount(0); await capture(page, token + '-archived-readonly');
    await page.getByRole('button', { name: 'Bỏ lưu trữ', exact: true }).click(); await save(page, root + '/' + id + '/unarchive', 'POST', 'Bỏ lưu trữ');
    expect(sql('Certification', id)[0].Status).toBe('Active'); await page.reload(); await expect(page.getByRole('button', { name: 'Ghi nhận gia hạn', exact: true })).toBeVisible();
    const current = (await response(page, root + '/' + id, 'GET')).body; expect(current.issuedOn).toBe('2026-10-25'); expect(current.expiresOn).toBe('2028-10-25');
    await capture(page, token + '-unarchived');
  } catch (error) { await capture(page, token + '-failed').catch(() => {}); throw error; }
  finally { await access.close(); }
});
