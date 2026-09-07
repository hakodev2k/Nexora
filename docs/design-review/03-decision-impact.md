# Major decision impact and recommendation register

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

All12groups remain **Open / Proposed**, not answers from PO. Existing canonical [feature decision queue](../features/90-open-decisions.md) retained. The design resolves small UX/technical choices but does not choose financial/privacy/irreversible/provider-cost policy for the customer. Ask one meaningful group when PO resumes decisions, not all minor screens.

## Q-01

**Context/problem:** Account deletion removes recoverable data and leaves backup/audit residuals.

**Options:** Grace period vs immediate irreversible purge; per-module export vs whole-account package.

**Recommended proposal:** 7-day cancellable grace is existing proposal, not approved; explicit residual disclosure.

**Trade-offs:** User safety vs retention/cost/complexity.

**Reference behavior and limit:** Trash reference products have differing retention, rejected for Nexora owner Trash; account deletion needs own policy. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** identity.User, owner tables/files/keys; FX01/08/10.

**Blocked:** Q-01 account purge/recovery commands. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-02

**Context/problem:** Step-up/MFA/recovery affect security friction and lost-access risk.

**Options:** MFA mandatory Admin only vs all accounts; recovery codes vs support-mediated recovery.

**Recommended proposal:** MFA Admin/SuperAdmin mandatory, User optional; recent-auth sensitive actions — existing proposal.

**Trade-offs:** Security vs onboarding/recovery burden.

**Reference behavior and limit:** Auth0 verification is not proof of identity/MFA; Bitwarden sensitive interactions adapted. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** Sessions/credentials/AccessSession/Vault; FX01/02/05/28.

**Blocked:** Q-02 sensitive authentication workflows. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-03

**Context/problem:** Sensitive share scope and restore/policy disable may expose data unexpectedly.

**Options:** Immediately block existing links on policy disable vs new-links-only; restore links manual vs auto.

**Recommended proposal:** Fail closed on disable; no automatic Trash link revival; safe field preview — proposal.

**Trade-offs:** Privacy/revocation vs continuity/user friction.

**Reference behavior and limit:** Drive audience dialog adapted; inherited edit/comment/children rejected. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** ShareLink/project/page/resource projections; FX04/08/27/31/37–40.

**Blocked:** Q-03 unresolved link revival/sensitive projections. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-04

**Context/problem:** Vault lost keys/operator access determines cryptographic and recovery design.

**Options:** Owner-held recovery vs operator-recoverable encryption; safe metadata exposure vs encrypted titles.

**Recommended proposal:** No Admin/Emergency reveal/copy/export; design explicit owner recovery/encrypted portability before key architecture.

**Trade-offs:** Recoverability vs operator decrypt capability/complexity.

**Reference behavior and limit:** Bitwarden item UI pattern does not authorize its key model or shared organization vault. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** vault tables/files/key inventory/Support; FX05/10/28.

**Blocked:** Q-04 key recovery, metadata, authenticated share/export. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-05

**Context/problem:** Budgets/FX/debts/corrections define meaning of money.

**Options:** Spending limits vs envelope; separate currency vs FX reports; reversal vs mutable correction.

**Recommended proposal:** Monthly category spending limits, separate currency reports, manual FX amounts, journaled corrections — proposal.

**Trade-offs:** Simplicity vs accounting capability; wrong choice changes totals/history.

**Reference behavior and limit:** Actual transfer linkage adapted; its budget/delete behavior not adopted. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** All finance tables/formulas/import/report screens; FX27.

**Blocked:** Q-05 ledger posting/corrections/budget/debt semantics. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-06

**Context/problem:** Shopee tracking needs legitimate available price source and refresh budget.

**Options:** Approved provider vs defer feature until feasible; exact item price vs voucher/shipping/member total.

**Recommended proposal:** One approved Shopee variant price provider,6h polling proposal, honest stale/unknown.

**Trade-offs:** Provider cost/terms/accuracy vs freshness; manual-only is not full tracking.

**Reference behavior and limit:** Keepa/Amazon chart pattern is not Shopee availability evidence. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** shopping tracker/observations/rules; FX30/31.

**Blocked:** Q-06 provider binding, price definition, refresh cost. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-07

**Context/problem:** Automation/network workflows can execute effects and leak data.

**Options:** Bounded step list vs richer DAG; inbound/outbound n8n scopes; public target only vs private network.

**Recommended proposal:** Trusted bounded linear/conditions<=20steps, explicit outbound projections and public guarded targets — proposal.

**Trade-offs:** Capability vs security/provider/network complexity.

**Reference behavior and limit:** n8n per-step debug adapted; code/AI/loops and raw payload exposure rejected. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** automation/connections/webhooks/monitor/network tools; FX29/32/34–36/38.

**Blocked:** Q-07 effect catalog, graph depth, egress and integrations. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-08

**Context/problem:** Capacity/log/backup targets incur ongoing cost and reliability obligations.

**Options:** Target users/storage/retention/RPO/RTO and provider budgets require owner expectations.

**Recommended proposal:** Discuss100concurrent,1GiB/User,RPO24h/RTO8h/backups30days — proposal only.

**Trade-offs:** Cost vs retention/durability/latency; approved indefinite owner Trash unchanged.

**Reference behavior and limit:** GitLab recovery separation and UptimeRobot monitoring patterns, not inherited SLA. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** Files/jobs/notifications/audit/recovery/provider polling; broad FX impact.

**Blocked:** Q-08 production quotas/SLO/retention/recovery. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-09

**Context/problem:** UI languages/default currency affect audience and financial entry.

**Options:** Vietnamese only vs Vietnamese+English; browser locale vs explicit initial setting.

**Recommended proposal:** Vietnamese+English, browser locale fallback Vietnamese; accounts explicit currency, VND default proposal.

**Trade-offs:** Translation effort vs audience usability.

**Reference behavior and limit:** Reference multilingual UI does not choose Nexora locales; timezone browser detection already confirmed. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** Profile/preferences/Finance formatting; FX01/09/27.

**Blocked:** Q-09 language/default currency; Monday delegated not reopened. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-10

**Context/problem:** Older optional productivity ideas may enlarge committed flow.

**Options:** Keep approved Task flow vs add standalone reminders/snooze/subtasks/recurrence/attachments.

**Recommended proposal:** Keep confirmed Task/Project/one-reminder baseline; add only explicitly selected extensions.

**Trade-offs:** Feature value vs lifecycle/scheduler/UI/data complexity.

**Reference behavior and limit:** Todoist/TickTick multiple/recurring patterns Future, not adopted automatically. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** Task/Reminder new entities/Calendar contracts; FX12/14.

**Blocked:** Q-10 optional extensions only. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-11

**Context/problem:** Document/Resume file conversion fidelity can lose structure/content.

**Options:** Markdown/HTML/assets/PDF subset vs DOCX roundtrip; unsupported-format report.

**Recommended proposal:** Markdown+assets or safeHTML+assets andPDF proposal; DOCX only explicit fidelity acceptance.

**Trade-offs:** Interoperability vs conversion cost/data loss.

**Reference behavior and limit:** Google Docs/Notion format behavior not automatic scope. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** Document versions/files/import/export/Resume; FX10/20/39.

**Blocked:** Q-11 formats/fidelity; manual Save unchanged. Other independent screens may be specified/refined, not coded without separate authorization.

## Q-12

**Context/problem:** Interview Calendar requirement conflicts with current two-source Calendar.

**Options:** Explicit linked ManualEvent vs third Interview projection source.

**Recommended proposal:** Owner-created linked ManualEvent with explicit coordinated reschedule preview — proposal.

**Trade-offs:** Avoid duplicate schedule/reminder vs extra synchronization surface.

**Reference behavior and limit:** Calendar reference not authority for new source; Job tracker does not decide it. Source evidence linked in [reference register](../ux-ui/references/product-reference-register.md); reference never approves policy.

**Affected data/architecture/modules:** career.Interview.CalendarResourceId/Calendar/ICS/reminders; FX13/39.

**Blocked:** Q-12 source/ownership/reschedule/cancel authority. Other independent screens may be specified/refined, not coded without separate authorization.
