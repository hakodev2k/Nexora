# Baseline/environment

Repository verified origin https://github.com/hakodev2k/Nexora.git; branch impl/m01-s00-scaffold; HEAD 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, dirty tree preserved (baseline-worktree.txt). Actual source identity612 paths in baseline-fingerprints.csv, SHA256309471C6BC07B5FBCAFE934FD02A776C0B1A616EA6E3FB70E9BB204E01BF22E7. No remote CI state assumed. QA authorization explicitly supersedes prior code-only amendment for this task; old audits are historical.

{
  "capturedUtc": "2026-10-18T14:02:51.7881039Z",
  "os": "Microsoft Windows NT 10.0.26200.0",
  "timezone": "SE Asia Standard Time",
  "dotnet": "10.0.302",
  "node": "v24.17.0",
  "npm": "11.13.0",
  "processorCount": 20,
  "files": 612
}

SQL Express2019 15.0.2000.5,SQL_Latin1_General_CP1_CI_AS; genuine engine, different from SQL2025 design profile.86 migration journal entries,0 disabled/untrusted checks,DBCC CHECKCONSTRAINTS0 violations (schema-verification.json). Windows native Edge155.0.4283.62, headless,DPR1;5 CSS viewports320x740/390x844/768x1024/1366x768/1920x1080. Viewport emulation is not physical device/screen-reader testing.

Production frontend preview for targeted route/axe/performance; existing functional suite requires Vite dev for /src/api.ts helpers and is reported separately. Both use real API/service/SQL; no global route mock. Offline and dropped-first-response are explicitly controlled boundaries; drop handler first forwards to real server and SQL. Component125 passes are jsdom/MSW only. No live provider/email/payment/production.

Default FX40 is Ready but SystemEnabled0/RegistrationEnabled0. Gated state observed first; test-only SuperAdmin preview/commit enabled system plus explicit A/B module grants. B later revoked through same real API before stale save. No raw SQL action grant or auth bypass used. Entire test database and private capture/file roots belong to the generated runtime marker; no existing developer DB read/writes.

Initial sandbox SQL SSPI and NuGet config access failed; approved host-principal execution and offline cached restore succeeded. Initial browser harness used wrong /auth/me route and invalid sqlcmd flags; corrected harness to existing /me and safe output parser, original attempts retained. Mobile final empty SQL result NULL parser corrected, original attempt retained; no product assertion weakened. Unit continuation uses -p:BuildProjectReferences=false to avoid locked live API DLL; original compile blocker still recorded.
