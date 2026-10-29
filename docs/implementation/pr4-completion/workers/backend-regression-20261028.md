# Final backend regression — 2026-10-28

Owner learning_completion; coordinator root. Existing PR4 branch `impl/m01-s00-scaffold`, local HEAD `a0215f7b3e85d33503cff9077b41cc065240cb81` plus delivered-increment worktree. Current user/DEC014 automated-QA authority; QA routing and evidence-grounded verification rules reloaded. Scope: complete current backend unit and real SQL suites, including new Support gate and trusted Focus-writer regressions. No product edits, provider activation, production data or shared API restart.

Commands: `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --logger trx ...`, followed sequentially by the equivalent IntegrationTests project command. Private local test connection was loaded from the owned f47 runtime environment without printing it; `SqlApiFixture` creates its own fresh GUID-named `Nexora_Test_` database, migration/replay/checksum/bootstrap checks and test host, then disposes only the database it created. The shared f47 browser database was neither reused as the test database nor reset.

| Suite | Executed | Passed | Failed/errors | Not executed | Evidence |
|---|---:|---:|---:|---:|---|
| Backend Unit Release |193|193|0|0|`artifacts/backend-final-20261028-c771208309394498927931157fdcb4b5/unit.trx`|
| Entire real SQL/API Integration Release |311|311|0|0|same directory `integration.trx`; test duration2m33s|

These are504 distinct backend cases across two suites in this run; not feature-coverage counts. Actual Integration TRX includes all3 User/Admin/SuperAdmin blocked-Support theory rows and all4 trusted-writer cases: concurrent/repeated finish, current-owner revocation/legitimate restoration, notification-insert rollback/retry, and authority-change transaction rollback. Support success is **not** proved: its current Blocked gate remains intact. Local notification persistence does not prove live channel delivery.

Source fingerprints: `SqlSupportConsentTests.cs` SHA256 `72091270182ACD2BCD9EABE2EB2528972E8E74A37867644FBE8F5F937D550BA1`; `SqlNotificationTrustedWriterTests.cs` `76DD60C82C048CEA60D116378E1F24442C13BEFAAB7C3B5BC136FD6F4893D35D`; `SqlApiFixture.cs` `64C4990E771035A7B3CDFF3D6A1DA3D9CC275DD2F7530F0C6592529D0C2ADA6C`. Unit TRX SHA256 `E00039921643598F7F4094ABA86A5E20EB3C866CD90EF7D0E9ACE40BD63E0A8C`; Integration TRX `F3C92E9771855FDEC89798DB32CD8D7F4BD4A5D0EB3B0FE4D678BD0FA617C01E`. Logs retained alongside TRX; no skips suppressed or assertions weakened.

Goal trace: NXG-SYS02/03/05/08/09/10/14; Support FX05BR001–005/AC001–003 negative availability boundaries; existing Focus phase finisher FX19BR001/002/004,AC003 and FX06BR001/002 trusted publication contract. Full browser/frontend regression, delivered-commit reconciliation, independent/human acceptance and D09 current-head CI remain separate required gates. Backend suites passing alone never means release ready.

Executable fingerprints captured immediately after this run (.NET SDK10.0.302): UnitTests DLL `DA1574A117F5EAF0086A969A74A5E6EC7525CA142153646322178BF3AF88950E`; IntegrationTests DLL `7E695CE79D72A30798E8A9FC4A8D2FA00C300BFC40AC3E73A2EA2953427EC2CE`; API DLL `9A789EDC45956FDF289C6BD01F644A6C0D5B6E731A84BADB1D74B4CD34E7678C`; Infrastructure DLL `EC1CDA03469981E4E06FDB2D64B13E4D3B291C7E09D57F8CA2F3BD557D0EBA17`. These identify tested Release binaries; they do not identify the separately running browser API binary.
