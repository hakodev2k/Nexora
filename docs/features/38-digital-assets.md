# Domains, Hosting, VPS, Certificates, Licenses và Services

FX-38 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Metadata tài sản số, expiry/renewal/cost/relations, optional observed checks.

[Cloudflare Registrar](https://developers.cloudflare.com/registrar/account-options/renew-domains/): Domain expiry/renewal và auto-renew là những thông tin cần phân biệt.

[Snipe-IT](https://snipe-it.readme.io/docs/overview): Asset và các thông tin quản lý liên quan được tổ chức thành records.

**Áp dụng cho Nexora:** Cloudflare tham chiếu domain lifecycle; Nexora lưu theo dõi, không remote renew/pay/deploy/control máy chủ.

**Màn hình:** `/digital-assets`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Create type/name/provider/identifier → renewal/expiry/cost/VaultRef.
2. List filter type/status/expiry; detail manual data và observed data nguồn/time riêng.
3. Configure lead-time reminder, Record Renewal thay expiry/history; optional networkchecks Q-07.

## Dữ liệu và validation

- Domain Unicode+punycode normalized, registrar/registered/expiry/autorenewinfo/nameservernotes.
- Hosting/VPS provider/plan/region/publicendpoint/privateconnection VaultRef/cost/renewal.
- Certificate subject/SAN/issuer/fingerprint/notBefore/notAfter/source; privatekey chỉ Vault.
- License product/vendor/edition/quantity/deviceassociation/purchase/expiry/keyRef; OnlineService plan/accountlabel/renewal/FinanceSubscriptionRef.

## Hành vi và lifecycle

- **FX-38-BR-001:** Status Active/Expired/Cancelled/Archived; Expired derived from expiry, cancel/archive explicit. Renewal không payment.
- **FX-38-BR-002:** AutoRenew flag informational, UI không báo đã trả tiền; timezone expiryinstant rõ.
- **FX-38-BR-003:** Manual/observed fields không overwrite nhau; RDAP/DNS/TLS approved provider+timestamp, errors degraded.
- **FX-38-BR-004:** Secrets/passwords/activationkeys chỉ VaultRef; URLs sanitize, private IP classified không public share.
- **FX-38-BR-005:** Delete giữ Vault/Finance records; expiry reminders sourceRevision dedupe all 3; monitoring feature36.
- **FX-38-BR-006:** Cross-currency costs separate; privacy projection Q-03 và network Q-07 trước implementation phụ thuộc.

## Quyền, API và tích hợp

- DigitalAssetTypeProvider/RecordRenewal/InspectPublicEndpoint/ExpiryIntent.
- Certificate parser input file bounded; remote inspection egress guard.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-38-AC-001:** Autorenew=true không gọi payment API.
- **FX-38-AC-002:** Observed TLSexpiry khác manual tạo discrepancy, không overwrite.
- **FX-38-AC-003:** Domain confusable/IDN hiển thị cả chuẩn hóa an toàn; privatekey không metadata.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-07-assets-and-career.md](../requirements/phases/phase-07-assets-and-career.md): `P07-CER-001`, `P07-CER-002`, `P07-CER-003`, `P07-DIG-001`, `P07-DIG-002`, `P07-DIG-003`, `P07-DIG-004`, `P07-DOM-001`, `P07-DOM-002`, `P07-INF-001`, `P07-INF-002`, `P07-LIC-001`, `P07-SVC-001`

Quyết định lớn cần PO: [Q-03](90-open-decisions.md#q-03), [Q-07](90-open-decisions.md#q-07). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
