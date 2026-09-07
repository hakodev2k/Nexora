# UX-01 — Information architecture

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

| Group | Entry points | Rationale |
| --- | --- | --- |
| Global | Home, Search, Quick Create, Notifications | Frequent cross-module attention/navigation |
| Productivity | Projects, Calendar, Planner, Goals, Habits, Time, Focus | Work capture/planning/execution; Tasks reached through Projects or source detail, no orphan Task area |
| Knowledge | Documents, Bookmarks, Snippets, Read Later | Documents includes Note/Knowledge types, not3menu modules |
| Finance & Security | Finance, Vault | Distinct data sensitivity; Vault never previewed inside Finance automatically |
| Information & Shopping | News; Price Tracking, Wishlist, Orders | Read/watch versus manually recorded purchases; subgroup Shopping collapsible |
| Developer & Automation | Toolbox, GitHub Discovery, Automation, Integrations, Monitoring | Tools separate from workflows/provider operations |
| Assets | Personal Assets, Digital Assets | Physical versus online metadata, shared pattern library |
| Career & Learning | Jobs, Resumes, Learning | Job process and evidence; Skills/Courses/Certifications inside Learning |
| Utilities | Files, Trash, Settings | Cross-module lifecycle and configuration |
| Privileged separate area | Users/Permissions, Modules, Audit, Jobs, Recovery, Emergency | Only qualified actors; no personal-user impersonation |


Collapsed groups default reduce cognitive load; currently active module group auto-expanded for discovery but User can collapse other groups. Pinned modules are display preferences, not grants. Search/More reveals all entitled modules; disabled deep links return safe unavailable with Back/Home. No40-item bottom navigation. FX identifiers are catalog entries, not mandatory top-level nav items.

Within Documents, Folder/page tree is module-local and bounded2levels each; within Project, Tasks Kanban/Table default Kanban. No duplicate global sidebar nesting all user resources. Admin uses separate navigation context and returns to own personal Home. Reference [Notion sidebar](https://www.notion.com/help/navigate-with-the-sidebar), adapted; Workspace/teamspace portions rejected. Reviewed2026-09-07.
