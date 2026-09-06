# UX-13 — Admin, Support & Emergency

Admin uses a distinct shell:
```text
NEXORA ADMIN
Users
Modules
Permissions
Jobs
Audit
System Settings
```

Support banner:
```text
SUPPORT MODE
User · Module · Expiry · Read-only
[End session]
```

Emergency banner:
```text
EMERGENCY ACCESS
User · Module · Reason · Expiry · Audit status
[End session]
```

No impersonation ambiguity. No mutation/export. No Vault reveal/copy. Out-of-scope navigation is clearly blocked.

High-impact role/module grant changes show before→after consequences before Save.