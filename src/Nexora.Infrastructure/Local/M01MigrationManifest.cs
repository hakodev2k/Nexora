namespace Nexora.Infrastructure.Local;

/// <summary>
/// The reviewed SQL migrations that make the approved local Release 1
/// surface executable. The list is intentionally explicit: a directory
/// wildcard must never cause an unreviewed migration to run.
/// </summary>
public static class M01MigrationManifest
{
    public static IReadOnlyList<string> RequiredFileNames { get; } =
    [
        "20260909_0001_m01_identity_platform.sql",
        "20260909_0002_bootstrap_closure.sql",
        "20260910_0002_r1_catalog_and_productivity.sql",
        "20260910_0003_productivity_lifecycle.sql",
        "20260910_0004_notifications_inbox.sql",
        "20260910_0005_preferences.sql",
        "20260910_0006_documents_pages.sql",
        "20260910_0007_action_catalog_alignment.sql",
        "20260910_0008_finance_manual_records.sql",
        "20260910_0009_bookmarks_manual.sql",
        "20260910_0010_snippets_manual.sql",
        "20260910_0011_reading_queue_bookmarks.sql",
        "20260910_0012_organization_tags.sql",
        "20260910_0013_developer_toolbox_pure.sql",
        "20260910_0014_goals_numeric.sql",
        "20260910_0015_dashboard_attention.sql",
        "20260910_0016_global_search_source_query.sql",
        "20260910_0017_favorites_refs.sql",
        "20260910_0018_local_runtime_catalog_gate.sql",
        "20260910_0019_productivity_contract_alignment.sql",
        "20260910_0020_task_calendar_projection.sql",
        "20260911_0021_core_sharing_support_files.sql",
        "20260911_0022_reminders_scheduling.sql",
        "20260911_0023_planner_habits.sql",
        "20260912_0024_planner_habits_owner_integrity.sql",
        "20260913_0025_review_hardening.sql",
        "20260913_0026_identity_local_delivery.sql",
        "20260922_0027_sanitize_session_device_labels.sql",
        "20260928_0028_bootstrap_ready_module_grants.sql",
        "20260929_0029_habit_trash_consistency.sql",
        "20260930_0030_access_preview_action.sql",
        "20261001_0031_trash_source_permissions.sql",
        "20261001_0032_time_tracking_local.sql",
        "20261001_0033_focus_local.sql",
        "20261002_0034_focus_conversion_time_purge.sql",
        "20261002_0035_wishlist_local.sql",
        "20261003_0036_learning_skills_local.sql",
        "20261003_0037_learning_courses_local.sql",
            "20261003_0038_personal_assets_local.sql",
            "20261003_0039_digital_assets_local.sql",
            "20261003_0040_career_manual.sql",
            "20261003_0041_calendar_ics_import.sql",
            "20261004_0042_calendar_ics_export.sql",
            "20261004_0043_sharing_policy_authority.sql",
            "20261004_0044_sharing_subset_readiness.sql",
            "20261004_0045_monitoring_http_config.sql"
    ];
}
