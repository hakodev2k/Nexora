#!/usr/bin/env python3
"""Static S00 checks for the Nexora local scaffold.

These checks intentionally do not claim application runtime or SQL integration
coverage. They guard the approved M01-S00 scaffold contract and paused-scope
boundaries until SQL-backed stories are implemented.
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
    "src/Nexora.Api/Security/CsrfTokenService.cs",
    "src/Nexora.Api/Security/SecurityHeadersMiddleware.cs",
    "src/Nexora.Domain/Nexora.Domain.csproj",
    "src/Nexora.Application/Nexora.Application.csproj",
    "tests/Nexora.UnitTests/Nexora.UnitTests.csproj",
    "tests/Nexora.UnitTests/Program.cs",
    "web/Nexora.Web/package.json",
    "web/Nexora.Web/src/App.tsx",
    "web/Nexora.Web/src/api.ts",
    "scripts/dev/doctor.sh",
    "scripts/dev/verify.sh",
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
    if "/api/v1/auth/csrf" not in program or "__Host-NexoraCsrf" not in program:
        fail("S00 backend must expose the approved getCsrf control endpoint with host-prefixed CSRF cookie")

    web_api = read("web/Nexora.Web/src/api.ts")
    if "let csrfToken" not in web_api or "localStorage" in web_api or "sessionStorage" in web_api:
        fail("frontend must keep CSRF token in memory and avoid browser storage")

    package = json.loads(read("web/Nexora.Web/package.json"))
    deps = package.get("dependencies", {}) | package.get("devDependencies", {})
    for dep in ("react", "react-dom", "vite", "typescript"):
        if dep not in deps:
            fail(f"frontend dependency missing: {dep}")

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

    print("S00 static verification passed: scaffold files, .NET 10 pin, CSRF memory boundary, unit harness, and paused-scope runtime guards are present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
