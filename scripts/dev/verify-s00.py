#!/usr/bin/env python3
"""Static checks for the local Release 1 implementation baseline.

This verifier is intentionally structural. It catches accidental reintroduction
of memory-backed authority, development-only admin bypasses, browser token
storage or provider execution. It does not run functional tests or claim runtime
verification; those activities are owned by the human implementation owner.
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
    "src/Nexora.Api/Features/Identity/IdentityContracts.cs",
    "src/Nexora.Api/Features/Modules/ModuleEndpoints.cs",
    "src/Nexora.Api/Features/Modules/ModuleContracts.cs",
    "src/Nexora.Api/Features/Access/AdminAccessEndpoints.cs",
    "src/Nexora.Api/Features/Access/AdminAccessContracts.cs",
    "src/Nexora.Api/Features/Notifications/NotificationEndpoints.cs",
    "src/Nexora.Api/Features/Notifications/NotificationContracts.cs",
    "src/Nexora.Api/Features/Trash/TrashEndpoints.cs",
    "src/Nexora.Api/Features/Trash/TrashContracts.cs",
    "src/Nexora.Api/Features/Settings/SettingsEndpoints.cs",
    "src/Nexora.Api/Features/Settings/SettingsContracts.cs",
    "src/Nexora.Api/Features/Documents/DocumentEndpoints.cs",
    "src/Nexora.Api/Features/Documents/DocumentContracts.cs",
    "src/Nexora.Api/Features/Productivity/ProductivityEndpoints.cs",
    "src/Nexora.Api/Features/Finance/FinanceEndpoints.cs",
    "src/Nexora.Api/Features/Finance/FinanceContracts.cs",
    "src/Nexora.Api/Features/Bookmarks/BookmarkEndpoints.cs",
    "src/Nexora.Api/Features/Bookmarks/BookmarkContracts.cs",
    "src/Nexora.Api/Features/Snippets/SnippetEndpoints.cs",
    "src/Nexora.Api/Features/Snippets/SnippetContracts.cs",
    "src/Nexora.Api/Features/Reading/ReadingEndpoints.cs",
    "src/Nexora.Api/Features/Reading/ReadingContracts.cs",
    "src/Nexora.Api/Http/ApiResult.cs",
    "src/Nexora.Api/Security/CsrfTokenService.cs",
    "src/Nexora.Api/Security/EndpointSecurityFilters.cs",
    "src/Nexora.Api/Security/SessionCookieService.cs",
    "src/Nexora.Domain/Nexora.Domain.csproj",
    "src/Nexora.Application/Nexora.Application.csproj",
    "src/Nexora.Application/Identity/IdentityServiceContracts.cs",
    "src/Nexora.Application/Modules/ModulePolicyServiceContracts.cs",
    "src/Nexora.Application/Access/AdminAccessServiceContracts.cs",
    "src/Nexora.Application/Notifications/NotificationServiceContracts.cs",
    "src/Nexora.Application/Trash/TrashServiceContracts.cs",
    "src/Nexora.Application/Settings/SettingsServiceContracts.cs",
    "src/Nexora.Application/Documents/DocumentServiceContracts.cs",
    "src/Nexora.Application/Productivity/ProductivityServiceContracts.cs",
    "src/Nexora.Application/Finance/FinanceServiceContracts.cs",
    "src/Nexora.Application/Bookmarks/BookmarkServiceContracts.cs",
    "src/Nexora.Application/Snippets/SnippetServiceContracts.cs",
    "src/Nexora.Application/Reading/ReadingServiceContracts.cs",
    "src/Nexora.Infrastructure/Nexora.Infrastructure.csproj",
    "src/Nexora.Infrastructure/Identity/SqlIdentityService.cs",
    "src/Nexora.Infrastructure/Modules/SqlModulePolicyService.cs",
    "src/Nexora.Infrastructure/Access/SqlAdminAccessService.cs",
    "src/Nexora.Infrastructure/Notifications/SqlNotificationService.cs",
    "src/Nexora.Infrastructure/Trash/SqlTrashService.cs",
    "src/Nexora.Infrastructure/Settings/SqlSettingsService.cs",
    "src/Nexora.Infrastructure/Documents/SqlDocumentService.cs",
    "src/Nexora.Infrastructure/Productivity/SqlProductivityService.cs",
    "src/Nexora.Infrastructure/Finance/SqlFinanceService.cs",
    "src/Nexora.Infrastructure/Bookmarks/SqlBookmarkService.cs",
    "src/Nexora.Infrastructure/Snippets/SqlSnippetService.cs",
    "src/Nexora.Infrastructure/Reading/SqlReadingService.cs",
    "src/Nexora.Infrastructure/Persistence/SqlConnectionFactory.cs",
    "database/migrations/20260909_0001_m01_identity_platform.sql",
    "database/migrations/20260910_0002_r1_catalog_and_productivity.sql",
    "database/migrations/20260910_0003_productivity_lifecycle.sql",
    "database/migrations/20260910_0004_notifications_inbox.sql",
    "database/migrations/20260910_0005_preferences.sql",
    "database/migrations/20260910_0006_documents_pages.sql",
    "database/migrations/20260910_0007_action_catalog_alignment.sql",
    "database/migrations/20260910_0008_finance_manual_records.sql",
    "database/migrations/20260910_0009_bookmarks_manual.sql",
    "database/migrations/20260910_0010_snippets_manual.sql",
    "database/migrations/20260910_0011_reading_queue_bookmarks.sql",
    "web/Nexora.Web/package.json",
    "web/Nexora.Web/src/App.tsx",
    "web/Nexora.Web/src/api.ts",
    "web/Nexora.Web/src/styles.css",
    "scripts/dev/doctor.sh",
    "scripts/dev/verify.sh",
]

IDENTITY_ROUTES = [
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

PRODUCTIVITY_MARKERS = [
    "/projects",
    "/tasks",
    "/calendar/events",
    "createProject",
    "createTask",
    "createEvent",
]

FORBIDDEN_RUNTIME_PATTERNS = [
    r"X-Nexora-Dev-SuperAdmin",
    r"RequireDevelopmentSuperAdminProof",
    r"listDevAccountMessages",
    r"localStorage",
    r"sessionStorage",
    r"Bearer ",
    r"(?i)HttpClient.*https?://",
]


def fail(message: str) -> None:
    print(f"local static verification failed: {message}", file=sys.stderr)
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
        fail("M01 is a delivery milestone, not a runtime API folder")
    if (ROOT / "src/Nexora.Api/Controllers").exists():
        fail("Minimal API runtime must not reintroduce MVC controllers")
    if (ROOT / "src/Nexora.Api/Security/ValidateCsrfAttribute.cs").exists():
        fail("controller-only CSRF attribute must not exist")

    for path in REQUIRED_FILES:
        read(path)

    global_json = json.loads(read("global.json"))
    sdk_version = global_json.get("sdk", {}).get("version", "")
    if not sdk_version.startswith("10."):
        fail(f"global.json must pin .NET 10 SDK, got {sdk_version!r}")

    csproj = read("src/Nexora.Api/Nexora.Api.csproj")
    if "<TargetFramework>net10.0</TargetFramework>" not in csproj:
        fail("Nexora.Api must target net10.0")
    for project in ("Nexora.Domain.csproj", "Nexora.Application.csproj", "Nexora.Infrastructure.csproj"):
        if project not in csproj:
            fail(f"Nexora.Api must reference {project}")

    program = read("src/Nexora.Api/Program.cs")
    for marker in ("MapIdentityEndpoints", "MapModuleEndpoints", "MapAdminAccessEndpoints", "MapNotificationEndpoints", "MapTrashEndpoints", "MapSettingsEndpoints", "MapDocumentEndpoints", "MapProductivityEndpoints", "MapFinanceEndpoints", "MapBookmarkEndpoints", "MapSnippetEndpoints", "MapReadingEndpoints", "IFinanceService", "SqlFinanceService", "IBookmarkService", "SqlBookmarkService", "ISnippetService", "SqlSnippetService", "IReadingService", "SqlReadingService", "IDocumentService", "SqlDocumentService", "IIdentityService", "SqlIdentityService", "SqlConnectionFactory"):
        if marker not in program:
            fail(f"Program.cs marker missing: {marker}")
    if "DevelopmentIdentityStore" in program or "DevelopmentModuleStore" in program:
        fail("memory stores must not be registered by the runtime")
    if "AddControllers" in program or "MapControllers" in program:
        fail("Program.cs must not register MVC controllers")

    endpoints = read("src/Nexora.Api/Features/Identity/IdentityEndpoints.cs")
    for route in IDENTITY_ROUTES:
        if route not in endpoints:
            fail(f"Identity route missing: {route}")
    csrf_service = read("src/Nexora.Api/Security/CsrfTokenService.cs")
    if "RequireCsrfForUnsafeMethods" not in endpoints or "__Host-NexoraCsrf" not in csrf_service:
        fail("Identity routes must use the shared CSRF group and host-prefixed CSRF cookie")
    if "__Host-NexoraSession" not in read("src/Nexora.Api/Security/SessionCookieService.cs"):
        fail("session cookie must be host-prefixed")

    module_endpoints = read("src/Nexora.Api/Features/Modules/ModuleEndpoints.cs")
    for marker in ("/api/v1/admin/modules", "listModules", "previewModule", "setModulePolicy", "IModulePolicyService", "GetPrincipal"):
        if marker not in module_endpoints:
            fail(f"module route marker missing: {marker}")

    admin_endpoints = read("src/Nexora.Api/Features/Access/AdminAccessEndpoints.cs")
    for marker in ("/users", "listAdminUsers", "setAdminUserRole", "setAdminActionGrant", "setAdminModuleGrant", "disableAdminUser", "IAdminAccessService"):
        if marker not in admin_endpoints:
            fail(f"admin route marker missing: {marker}")

    productivity_endpoints = read("src/Nexora.Api/Features/Productivity/ProductivityEndpoints.cs")
    for marker in PRODUCTIVITY_MARKERS + ["transitionProject", "transitionTask", "transitionEvent"]:
        if marker not in productivity_endpoints:
            fail(f"productivity route marker missing: {marker}")

    document_endpoints = read("src/Nexora.Api/Features/Documents/DocumentEndpoints.cs")
    for marker in ("/documents", "listDocuments", "getDocument", "createDocument", "saveDocument", "transitionDocument", "IDocumentService"):
        if marker not in document_endpoints:
            fail(f"document route marker missing: {marker}")

    finance_endpoints = read("src/Nexora.Api/Features/Finance/FinanceEndpoints.cs")
    for marker in ("/finance/categories", "/finance/records", "listFinanceCategories", "createFinanceRecord", "updateFinanceRecord", "IFinanceService"):
        if marker not in finance_endpoints:
            fail(f"finance route marker missing: {marker}")

    bookmark_endpoints = read("src/Nexora.Api/Features/Bookmarks/BookmarkEndpoints.cs")
    for marker in ("/bookmarks", "listBookmarks", "createBookmark", "updateBookmark", "transitionBookmark", "IBookmarkService"):
        if marker not in bookmark_endpoints:
            fail(f"bookmark route marker missing: {marker}")

    snippet_endpoints = read("src/Nexora.Api/Features/Snippets/SnippetEndpoints.cs")
    for marker in ("/snippets", "listSnippets", "createSnippet", "saveSnippet", "transitionSnippet", "ISnippetService"):
        if marker not in snippet_endpoints:
            fail(f"snippet route marker missing: {marker}")

    reading_endpoints = read("src/Nexora.Api/Features/Reading/ReadingEndpoints.cs")
    for marker in ("/read-later", "listReadingQueue", "saveReadingItem", "removeReadingItem", "updateReadingItem", "IReadingService"):
        if marker not in reading_endpoints:
            fail(f"reading route marker missing: {marker}")

    filters = read("src/Nexora.Api/Security/EndpointSecurityFilters.cs")
    if "RequireCsrfForUnsafeMethods" not in filters or "X-CSRF-Token" not in filters:
        fail("shared CSRF filter is missing")
    if "X-Nexora-Dev-SuperAdmin" in filters:
        fail("development SuperAdmin header bypass must not exist")

    web_api = read("web/Nexora.Web/src/api.ts")
    if "let csrfToken" not in web_api or "localStorage" in web_api or "sessionStorage" in web_api:
        fail("frontend must keep CSRF in memory and never store auth tokens in browser storage")
    app = read("web/Nexora.Web/src/App.tsx")
    if "demoPassword" in app or "listDevAccountMessages" in app or "X-Nexora-Dev-SuperAdmin" in app:
        fail("frontend must not depend on demo accounts, dev mailbox endpoint or admin proof header")

    package = json.loads(read("web/Nexora.Web/package.json"))
    deps = package.get("dependencies", {}) | package.get("devDependencies", {})
    for dep in ("react", "react-dom", "vite", "typescript", "@types/react", "@types/react-dom"):
        if dep not in deps:
            fail(f"frontend dependency missing: {dep}")

    migration = read("database/migrations/20260909_0001_m01_identity_platform.sql")
    for table in ("[identity].[User]", "[identity].[Session]", "[identity].[OneTimeToken]", "[platform].[PersonalSpace]", "[security].[AuditEvent]", "[operations].[Outbox]"):
        if table not in migration:
            fail(f"identity migration table missing: {table}")
    migration2 = read("database/migrations/20260910_0002_r1_catalog_and_productivity.sql")
    for table in ("[platform].[Module]", "[productivity].[Project]", "[productivity].[Task]", "[calendar].[Event]"):
        if table not in migration2:
            fail(f"Release 1 migration table missing: {table}")
    migration3 = read("database/migrations/20260910_0003_productivity_lifecycle.sql")
    for table in ("[productivity].[ProjectHistory]", "[productivity].[TaskHistory]", "[platform].[TrashItem]"):
        if table not in migration3:
            fail(f"productivity lifecycle table missing: {table}")
    migration4 = read("database/migrations/20260910_0004_notifications_inbox.sql")
    for marker in ("[notifications].[Notification]", "[notifications].[Delivery]", "[RowVersion]", "BrowserPush"):
        if marker not in migration4:
            fail(f"notification migration marker missing: {marker}")
    if "[platform].[TrashItem]" not in migration3:
        fail("Trash migration table missing")
    migration5 = read("database/migrations/20260910_0005_preferences.sql")
    for marker in ("[platform].[Preference]", "[RowVersion]", "[ValueJson]"):
        if marker not in migration5:
            fail(f"preference migration marker missing: {marker}")
    migration6 = read("database/migrations/20260910_0006_documents_pages.sql")
    for marker in ("[documents].[Page]", "[documents].[PageVersion]", "[DocumentType]", "[EditorMode]", "[VersionNumber]"):
        if marker not in migration6:
            fail(f"document migration marker missing: {marker}")
    migration8 = read("database/migrations/20260910_0008_finance_manual_records.sql")
    for marker in ("[finance].[ManualCategory]", "[finance].[ManualRecord]", "[Amount]", "[CurrencyCode]", "[OccurredOn]"):
        if marker not in migration8:
            fail(f"finance migration marker missing: {marker}")
    migration9 = read("database/migrations/20260910_0009_bookmarks_manual.sql")
    for marker in ("[knowledge].[Bookmark]", "[CanonicalUrl]", "[UrlDigest]", "[MetadataJson]", "[Status]"):
        if marker not in migration9:
            fail(f"bookmark migration marker missing: {marker}")
    migration10 = read("database/migrations/20260910_0010_snippets_manual.sql")
    for marker in ("[knowledge].[Snippet]", "[knowledge].[SnippetVersion]", "[SourceText]", "[CurrentVersion]", "snippets.snippet.save"):
        if marker not in migration10:
            fail(f"snippet migration marker missing: {marker}")
    migration11 = read("database/migrations/20260910_0011_reading_queue_bookmarks.sql")
    for marker in ("[knowledge].[ReadingItem]", "[SourceType]", "[SourceId]", "[SafeTitleSnapshot]", "[SafeUrlSnapshot]", "reading.item.position"):
        if marker not in migration11:
            fail(f"reading migration marker missing: {marker}")

    scanned = []
    for pattern in ("src/Nexora.Api/**/*.cs", "src/Nexora.Infrastructure/**/*.cs", "web/Nexora.Web/src/**/*"):
        scanned.extend(ROOT.glob(pattern))
    for path in scanned:
        if not path.is_file():
            continue
        text = path.read_text(encoding="utf-8")
        rel = path.relative_to(ROOT)
        for forbidden in FORBIDDEN_RUNTIME_PATTERNS:
            if re.search(forbidden, text):
                fail(f"forbidden runtime pattern {forbidden!r} in {rel}")

    print("local static verification passed: SQL-backed identity/module/access/notification/trash/settings/documents/productivity/finance/bookmarks/snippets/read-later runtime, CSRF/session boundary, owner-scoped migrations and no development admin/provider/browser-token bypass.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
