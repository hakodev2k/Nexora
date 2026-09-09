#!/usr/bin/env python3
"""Static S00 checks for the Nexora local scaffold.

These checks intentionally do not claim application runtime or SQL integration
coverage. They guard the approved M01-S00 scaffold contract, controller-based
Identity API route surface, and paused-scope boundaries until SQL-backed stories
are fully verified.
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
    "src/Nexora.Api/Controllers/AuthController.cs",
    "src/Nexora.Api/Controllers/MeController.cs",
    "src/Nexora.Api/Controllers/DevAccountMessagesController.cs",
    "src/Nexora.Api/Features/Identity/DevelopmentIdentityStore.cs",
    "src/Nexora.Api/Features/Identity/IdentityContracts.cs",
    "src/Nexora.Api/Http/ApiResult.cs",
    "src/Nexora.Api/Security/CsrfTokenService.cs",
    "src/Nexora.Api/Security/PasswordHashService.cs",
    "src/Nexora.Api/Security/SessionCookieService.cs",
    "src/Nexora.Api/Security/SecurityHeadersMiddleware.cs",
    "src/Nexora.Api/Security/ValidateCsrfAttribute.cs",
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

AUTH_ROUTE_MARKERS = [
    "[Route(\"api/v1/auth\")]",
    "[HttpGet(\"csrf\"",
    "[HttpPost(\"registrations\"",
    "[HttpPost(\"verifications\"",
    "[HttpPost(\"verifications/resend\"",
    "[HttpPost(\"login\"",
    "[HttpPost(\"logout\"",
    "[HttpPost(\"reauth\"",
    "[HttpPost(\"password-resets\"",
    "[HttpPost(\"password-resets/confirm\"",
]

ME_ROUTE_MARKERS = [
    "[Route(\"api/v1/me\")]",
    "[HttpGet(Name = \"getMe\")]",
    "[HttpPatch(Name = \"updateMe\")]",
    "[HttpGet(\"sessions\"",
    "[HttpDelete(\"sessions/{sessionId:guid}\"",
    "[HttpPost(\"sessions/revoke-all\"",
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
    if (ROOT / "src/Nexora.Api/M01").exists():
        fail("M01 is a delivery milestone, not a runtime API folder; src/Nexora.Api/M01 must not exist")
    if (ROOT / "src/Nexora.Api/Features/Identity/IdentityEndpoints.cs").exists():
        fail("Identity API must be controller-based; minimal endpoint extension must not exist")

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
    if "AddControllers" not in program or "MapControllers" not in program:
        fail("Program.cs must register and map MVC controllers")
    if "MapIdentityEndpoints" in program or "MapM01IdentityEndpoints" in program:
        fail("Program.cs must not use milestone/minimal endpoint extensions for Identity")

    auth = read("src/Nexora.Api/Controllers/AuthController.cs")
    for marker in AUTH_ROUTE_MARKERS:
        if marker not in auth:
            fail(f"AuthController marker missing: {marker}")
    if "[ApiController]" not in auth or "[ValidateCsrf]" not in auth:
        fail("AuthController must use ApiController and ValidateCsrf attributes")
    if "__Host-NexoraCsrf" not in auth:
        fail("CSRF controller action must set the host-prefixed CSRF cookie")

    me = read("src/Nexora.Api/Controllers/MeController.cs")
    for marker in ME_ROUTE_MARKERS:
        if marker not in me:
            fail(f"MeController marker missing: {marker}")
    if "[ApiController]" not in me or "[ValidateCsrf]" not in me:
        fail("MeController must use ApiController and ValidateCsrf attributes")

    dev = read("src/Nexora.Api/Controllers/DevAccountMessagesController.cs")
    if "api/v1/dev/account-messages" not in dev or "IsDevelopment()" not in dev:
        fail("development account-message controller must be development-gated")

    if "__Host-NexoraSession" not in read("src/Nexora.Api/Security/SessionCookieService.cs"):
        fail("M01 session cookie service must use the host-prefixed session cookie")

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

    # Only scan executable runtime surfaces for forbidden paused/provider behavior.
    # Domain policy and unit-test code may intentionally mention paused action keys
    # to prove that those actions are blocked.
    scanned = []
    for pattern in ("src/Nexora.Api/**/*.cs", "web/Nexora.Web/src/**/*"):
        scanned.extend(ROOT.glob(pattern))
    for path in scanned:
        if path.is_file():
            text = path.read_text(encoding="utf-8")
            rel = path.relative_to(ROOT)
            for forbidden in FORBIDDEN_RUNTIME_PATTERNS:
                if re.search(forbidden, text):
                    fail(f"forbidden runtime pattern {forbidden!r} in {rel}")

    print("S00 static verification passed: controller-based Identity API routes, .NET 10 pin, CSRF/session memory boundary, SQL artifact, and paused-scope guards are present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
