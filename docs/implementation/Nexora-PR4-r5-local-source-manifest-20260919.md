# Nexora PR #4 R5 — local source manifest

## Identity and limits

| Field | Value |
| --- | --- |
| Generated | 2026-09-19 local worktree snapshot |
| Branch | `impl/m01-s00-scaffold` |
| Normative main | `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` |
| Reviewed implementation parent / local HEAD | `aada36f089ca0ea85424dfa249a2228d70ca740b` |
| State | Uncommitted local remediation; no commit or push was authorized. |
| Final source verification | API, unit-test and SQL/API-integration projects built Release/no-restore with exit 0 and 0 warnings/errors. |
| Functional evidence | Historical pre-final-retention-hardening only; see the R5 report freshness addendum. |

This is an inventory of reviewable source and handoff files, excluding generated
`artifacts/` directories. SHA-256 values bind this exact local snapshot without
including any sensitive test inputs. It is not a Git commit and does not imply a
remote branch update, CI run, runtime verification or independent review.

| Path | SHA-256 |
| --- | --- |
| `.github/workflows/m01-ci.yml` | `a8a54785b682a8148c478e0460232603af6b2c1dea49eb0faa735f5f65dfe8a6` |
| `scripts/dev/test.ps1` | `c7f8ae4e9091905e3823840f1affa3f5de97b2bdf7378764cee90aa3a0310d54` |
| `scripts/dev/test.sh` | `9cc47d45f24851a84bb0ee74f851c0198bedf9ccd7850e822319e6e6093379b2` |
| `scripts/dev/test-e2e.ps1` | `91429101b1deb2ccbe4ec11eea45822e50fe99be82c8bdccbd3aa279deb6484f` |
| `scripts/dev/test-e2e.sh` | `148449fe7d06d76db16ae5ba8f37b53038635e51f9b5096c58adedcf1681892d` |
| `scripts/dev/test-filesystem.ps1` | `0b583665a42691ab26b0aa161fe83461939b1669eacf9851a57a3ee5a7c078a8` |
| `scripts/dev/test-filesystem.sh` | `097c71faa78b5ed39d130659d78baeafb188326208fa762d8721cef27554cbc8` |
| `scripts/dev/test-sql.ps1` | `024fc4ff8c55feeb3784b9f8ab9b04c8d7e01d577a53381d2c82e4cefb1f36fe` |
| `scripts/dev/test-sql.sh` | `517fab73607c080a363ee8dafc911a87b8909642d9f737dc5304557b93faff5c` |
| `src/Nexora.Infrastructure/Identity/LocalAccountMessageSink.cs` | `7848874a4ff5365a96295367c5d0e559f1ddc4b72554d17ed6ec60584e62b9ed` |
| `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs` | `435e37aa76723239d4adcdca738d8e07febad757b1b3a8d21a13ee6f501ef492` |
| `tests/Nexora.UnitTests/M01PolicyAndCaptureContractTests.cs` | `b0e3b4e9970900b13014a2dc568e3d92bef911e7f8ff4ff48ef259ad2dca9153` |
| `tests/Nexora.UnitTests/Nexora.UnitTests.csproj` | `0d4ee35a044ed52adfbd483c300b54dc913602af2f5c721e29eb09b086e63e0e` |
| `tests/Nexora.UnitTests/Program.cs` | `8afd31eaf558830a27125e3ed2309de258aef5e403fd451cfff1f8d76c135b2d` |
| `tests/Nexora.UnitTests/TestRunner.cs` | `5e3e29c4fea8549bd83667a2cc4aa57ae5bfe10217026cba5c5d6cf7b496d860` |
| `tests/Nexora.UnitTests/packages.lock.json` | `fba976f91df5efcd571864c9c8dc5b10915e147b74ac4545e4c6a1d4606897b0` |
| `tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj` | `38dd9a110612e27983b6d9cc5b3b8da40886813c866dbd3894f24c19fc50d86e` |
| `tests/Nexora.IntegrationTests/Program.cs` | `f357b73235a4570d013acb0544a0639d8bf0cf9a646e0a76346a4a44b1d7dc6c` |
| `tests/Nexora.IntegrationTests/SqlApiFixture.cs` | `0fdd6bae0fe7783bbec65c6bd0d81154cc4af703008f6d16a3a84a7df85d0243` |
| `tests/Nexora.IntegrationTests/SqlApiIntegrationTests.cs` | `1dba0663c3263c48248b589317faa6f8bf73213bf8f9aca23a17419cdaa404c4` |
| `tests/Nexora.IntegrationTests/packages.lock.json` | `f6c3f40d4665ca4f7acfc8e00edad854d7561b23e9b33df254024830de552cd8` |
| `web/Nexora.Web/package.json` | `2ae60c73b9192564fa8cfb6326830733982b842a5b162a31f446d069c9c696ce` |
| `web/Nexora.Web/package-lock.json` | `393e362c8c01e217719721f14e8fccf6326523a27324cd8d8f8e234d0c3dc595` |
| `web/Nexora.Web/src/App.tsx` | `a50dbb4c4e90cb5bd16decd22cd7cff54803d531d49c5c546e6f5a466becc034` |
| `web/Nexora.Web/src/App.test.tsx` | `ea9b41813f5e93e7525a5bd93688b0f12f0f010c753c271488a68ef2a7c15c5b` |
| `web/Nexora.Web/src/test/setup.ts` | `4a14948f38bdfb3d9e33813de4f431ab816b95941d0cfe4269c15f00656f3130` |
| `web/Nexora.Web/src/test/server.ts` | `c922f23191b09daa9a0671c7494b6af22139657002147e53f6dfc74b5c9a0165` |
| `web/Nexora.Web/vitest.config.ts` | `7fc3bb8579004e2a7efd3f49a30312dbc00751804f0f6b13acf06d35eea60868` |
| `web/Nexora.Web/playwright.config.ts` | `783613bf299ef6a4dff2c6dedba7895c40c7a33d48847c093f3f5dfd6f3cffb5` |
| `web/Nexora.Web/e2e/m01-identity.spec.ts` | `b8f067acf8f5c63989b5cbd9b8726efe09ecebb457cf140b17c2e8a1e9401b55` |
| `docs/implementation/Nexora-PR4-r5-remediation-test-engineering-main-aada36f-vi.md` | `3e0862e1719a7a95974fa12a6f715231332726cf7cd5b8a8cade72f9668d5394` |
| `docs/implementation/Nexora-PR4-r5-test-runbook-vi.md` | `4fa6c05c70209efca051173ec3e0d501082517a5ff5fdabc5f7ed38a540a07ab` |
| `docs/implementation/pr4-r5-description-draft-20260919.md` | `dbb8ba9b43c711e1938cba36647184a4b932f55abd0b4ad2d2908af5944ded5e` |
