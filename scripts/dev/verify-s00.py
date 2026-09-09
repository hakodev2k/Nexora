#!/usr/bin/env python3
"""Static S00 checks for the Nexora local scaffold.

These checks intentionally do not claim application runtime or SQL integration
coverage. They guard the approved M01-S00 scaffold contract, actual identity API
route surface, and paused-scope boundaries until SQL-backed stories are fully
verified.
"""
from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]

REQUIRED_FILES = [
    "global.json",
    "Directory.Build.props",
    "docker-compose.local.yml",
    "src/Nexora.Api/Nexora.Api.csproj",
    "src/Nexora.Api/Program.cs",
    "src/Nexora.Api/Features/Identity/IdentityEndpoints.cs",
    "src/Nexora.Api/Features/Identity/DevelopmentIdentityStore.cs",
    "src/Nexora.Api/Features/Identity/IdentityContracts.cs",
    "src/Nexora.Api/Http/ApiResult.cs",
    "src/Nexora.Api/Security/PasswordHashService.cs",
    "src/Nexora.Api/Security/SessionCookieService.cs",
    "src/Nexora.Api/Security/CsrfTokenService.cs",
    "src/Nexora.Api/Security/SecurityHeadersMiddleware.cs",
    "src/Nexora.Domain/Nexora.Domain.csproj",
    "src/Nexora.Application/Nexora.Application.csproj",
    "tests/Nexora.UnitTests/Nexora.UnitTests.csproj",
    "tests/Nexora.UnitTests/Program.cs",
    "database/migrations/20260909_0001_m01_identity_platform.sql",
    "web/Nexora.Web/package.json",
    "web/Nexora.Web/src/App.tsx",
    "web/Nexora.Web/src/api.ts",
    "scripts/dev/doctor.sh",
    "scripts/dev/verify.sh",
]

REQUIRED_API_ROUTES = [
    "/api/v1",
    "/auth/csrf",
    "/auth/registrations",
    "/auth/verifications",
    "/auth/verifications/resend",
    "/auth/login",
    "/auth/logout",
    "/auth/reauth",
    "/auth/password-resets",
    "/auth/password-resets/confirm",
    "/me",
    "/me/sessions",
    "/me/sessions/revoke-all",
]

FORBIDDEN_RUNTIME_PATTERNS = [
    r"prices\.",
    r"automation\.",
    r"integrations\.",
    r"localStorage",
    r"sessionStorage",
    r"Bearer ",
]


def fail(message: str) -> None:
    print(f"S00 static verification failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        fail(f"missing required file: {path}")
    text = target.read_text(encoding="utf-8")
    if not text.strip():
        fail(f"empty required file: {path}")
    return text


def main() -> int:
    legacy_milestone_api_dir = ROOT / "src/Nexora.Api/M01"
    if legacy_milestone_api_dir.exists():
        fail("Backend API code must be organized by feature/domain, not under src/Nexora.Api/M01")

    for path in REQUIRED_FILES:
        read(path)

    global_json = json.loads(read("global.json"))
    sdk_version = global_json.get("sdk", {}).get("version", "")
    if not sdk_version.startswith("10."):
        fail(f"global.json must pin .NET 10 SDK, got {sdk_version!r}")

    csproj = read("src/Nexora.Api/Nexora.Api.csproj")
    if "<TargetFramework>net10.0</TargetFramework>" not in csproj:
        fail("Nexora.Api must target net10.0")
    if "Nexora.Domain.csproj" not in csproj or "Nexora.Application.csproj" not in csproj:
        fail("Nexora.Api must reference the domain and application policy projects")

    program = read("src/Nexora.Api/Program.cs")
    if "MapIdentityEndpoints" not in program or "MapM01IdentityEndpoints" in program:
        fail("Program.cs must map feature-based identity endpoints, not milestone-named endpoints")
    if "Nexora.Api.M01" in program:
        fail("Program.cs must not reference a milestone-named API namespace")

    endpoints = read("src/Nexora.Api/Features/Identity/IdentityEndpoints.cs")
    for route in REQUIRED_API_ROUTES:
        if route not in endpoints:
            fail(f"Identity endpoint route missing: {route}")
    if "__Host-NexoraCsrf" not in endpoints:
        fail("CSRF endpoint must set the host-prefixed CSRF cookie")
    if "__Host-NexoraSession" not in read("src/Nexora.Api/Security/SessionCookieService.cs"):
        fail("Session cookie service must use the host-prefixed session cookie")

    web_api = read("web/Nexora.Web/src/api.ts")
    if "let csrfToken" not in web_api or "localStorage" in web_api or "sessionStorage" in web_api:
        fail("frontend must keep CSRF token in memory and avoid browser storage")

    package = json.loads(read("web/Nexora.Web/package.json"))
    deps = package.get("dependencies", {}) | package.get("devDependencies", {})
    for dep in ("react", "react-dom", "vite", "typescript"):
        if dep not in deps:
            fail(f"frontend dependency missing: {dep}")

    migration = read("database/migrations/20260909_0001_m01_identity_platform.sql")
    for table in ("[identity].[User]", "[identity].[Session]", "[identity].[OneTimeToken]", "[platform].[PersonalSpace]", "[security].[AuditEvent]", "[operations].[Outbox]"):
        if table not in migration:
            fail(f"M01 SQL migration table missing: {table}")

    scanned = []
    for pattern in ("src/Nexora.Api/**/*.cs", "web/Nexora.Web/src/**/*"):
        scanned.extend(ROOT.glob(pattern))
    for path in scanned:
        if path.is_file():
            text = path.read_text(encoding="utf-8")
            rel = path.relative_to(ROOT)
            if "namespace Nexora.Api.M01" in text:
                fail(f"milestone-named API namespace remains in {rel}")
            for forbidden in FORBIDDEN_RUNTIME_PATTERNS:
                if re.search(forbidden, text):
                    fail(f"forbidden runtime pattern {forbidden!r} in {rel}")

    print("S00 static verification passed: feature-based API layout, .NET 10 pin, identity API routes, CSRF/session memory boundary, SQL artifact, and paused-scope guards are present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
