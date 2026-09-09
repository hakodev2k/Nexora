# Reference evidence — handoff research

Tra cứu 2026-09-08. Chỉ documentation/release pages chính thức; chưa đăng nhập sản phẩm, chạy benchmark hay kiểm thử package compatibility. Không copy toàn bộ workflow sản phẩm ngoài vào Nexora. Ngày release dưới đây là dữ liệu nguồn; ngày tra cứu không là ngày chứng minh runtime.

| ID | Nguồn | Quan sát có bằng chứng | Apply / Adapt / Reject cho Nexora | Giới hạn |
| --- | --- | --- | --- | --- |
| REF-H01 | [GitHub 2FA recovery](https://docs.github.com/en/authentication/securing-your-account-with-two-factor-authentication-2fa/recovering-your-account-if-you-lose-your-2fa-credentials) | Recovery dựa trên codes/factor thay thế; email có thể khởi đầu verification nhưng không tự đủ proof | Adapt: proposal recovery codes; Reject tự thêm SSH/PAT/passkey vào Nexora | P-H01 chưa approved; chưa thử account recovery |
| REF-H02 | [OWASP MFA](https://cheatsheetseries.owasp.org/cheatsheets/Multifactor_Authentication_Cheat_Sheet.html) | Recovery/factor replacement cần được bảo vệ như authentication | Apply fail-closed và independent proof; không dùng session/email reset tự bỏ MFA | Không thay PO quyết định support identity proof |
| REF-H03 | [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html) | Standard problem details for HTTP errors | Apply structure; Nexora tự định code/field errors/safe messages | Không coi HTTP status là authorization |
| REF-H04 | [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110.html#name-if-match) | Preconditions dùng entity tags | Apply If-Match; Adapt aggregate access revision thay từng row permissions | Không tự resolve lost-update |
| REF-H05 | [EF concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) | Optimistic concurrency kiểm tra token khi update | Adapt SQL rowversion → opaque ETag; catch conflict, không auto-overwrite | Phải verify trên SQL thật sau approval |
| REF-H06 | [SQL restore](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/restore-and-recovery-overview-sql-server?view=sql-server-ver17) | Restore/recovery có sequence và dependencies | Adapt manifest gồm SQL/files/keys và rehearsal restore | Backup có mặt chưa chứng minh RPO/RTO |
| REF-H07 | [.NET support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [SDK downloads](https://dotnet.microsoft.com/en-us/download) | .NET10 LTS; SDK10.0.400/runtime10.0.11 tại thời điểm đọc | Choose research pin, security refresh trước scaffold | Không hứa pin vẫn mới vào ngày code |
| REF-H08 | [Node24.20.0](https://nodejs.org/en/blog/release/v24.20.0) | LTS release 2026-08-26, npm11.19.0 | Choose local tooling pin; npm lockfile | Không yêu cầu Corepack/pnpm |
| REF-H09 | [SQL2025 build list](https://learn.microsoft.com/en-us/troubleshoot/sql/releases/sqlserver-2025/build-versions) | CU8 17.0.4075.5, 2026-08-13 | Choose Developer local research pin | Developer edition không production license |
| REF-H10 | [React releases](https://github.com/react/react/releases) | 19.2.8 stable release listed 2026-07-21 | Choose SPA React/react-dom matched pin | Không thêm React Server Components |
| REF-H11 | [Vite releases](https://github.com/vitejs/vite/releases), [Vite8](https://vite.dev/blog/announcing-vite8) | v8.2.2 stable, v8.3 beta tách riêng; build pipeline thay đổi ở8 | Choose stable8.2.2; reject beta; compatibility gate S00 | React plugin/TS transitive pins cần lockfile thử sau approval |
| REF-H12 | [Google Docs history](https://support.google.com/docs/answer/190843?hl=en), [Notion editing](https://www.notion.com/help/writing-and-editing-basics) | History/navigation/block editing references từ feature baseline | Adapt editor/history; Reject autosave, realtime, unlimited nesting | Không đổi manual Save, DOCX/MD scope đã PO chốt |

Proposal recovery, RPO/RTO và workload là phân tích của Nexora, không gán các con số đó cho nguồn tham khảo.
