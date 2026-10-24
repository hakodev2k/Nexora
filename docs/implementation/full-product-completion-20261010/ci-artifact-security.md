# CI artifact boundary — bounded source slice

Source base: PR #4 `f8001aade70d5ee6aa0beebe38b6e67295544ac4`; requirements main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Authority: authorized security fixes; latest user AGENTS code-only. Goals NXG-SYS-08/14. Accountable owner root; independent reviewer `/root/baseline_review`.

Confirmed finding: `.github/workflows/m01-ci.yml` uploaded all `web/Nexora.Web/artifacts` in frontend and browser jobs. The browser step said “JUnit evidence only”, but selection also included any raw JSON, Playwright output/error contexts or private artifacts present in that directory. No actual disclosure is established by this source finding.

Fix: select only `vitest-junit.xml` and `coverage/cobertura-coverage.xml` for frontend, `playwright-junit.xml` for the standard browser job. Paths verified against Vitest config, package scripts and standard Playwright config. Functional regression uses a different configuration/report and is not run by this CI command. No test command, environment prerequisite, timeout or failure gate changes; no test/fixture/runtime creation.

Acceptance: filename allowlist matches actual configured reporters; no directory-wide upload; existing gates retained. Independent review found no blocking issue and `git diff --check` passed. CI execution on the new revision is pending; this source review does not prove application behavior or payload sanitization.

Potential residual risk: JUnit diagnostics/stdout may contain sensitive values. Narrow filename selection does not sanitize them. Reporter/log redaction requires a separate evidenced review; no whole-product security acceptance claimed.

Rollback: revert these three upload-path changes. No migration, account/grant change, provider effect or private runtime file is included.
