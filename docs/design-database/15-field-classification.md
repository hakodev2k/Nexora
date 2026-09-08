# Per-field mapping and sensitivity

> **Current decision amendment — 2026-09-07:** Physical delta17 is current for User/ShareLink/Vault flags, recovery wraps, four new tables and Career CalendarLink replacement. Baseline field counts/encryption/purge/Interview proposals are superseded only where specified; no migration executed. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Classification is a conservative design default, not approval of Support/share projection. Explicit allowlist projection must still be approved for sensitive modules; do not infer exposure from a column being called metadata. Nullable .NET mappings use nullable type/annotation. Classification is split by domain for reviewability.

<a id="identity-user"></a>
- [identity.User](classification/02-core-identity-platform.md#identity-user)

<a id="identity-session"></a>
- [identity.Session](classification/02-core-identity-platform.md#identity-session)

<a id="identity-onetimetoken"></a>
- [identity.OneTimeToken](classification/02-core-identity-platform.md#identity-onetimetoken)

<a id="identity-role"></a>
- [identity.Role](classification/02-core-identity-platform.md#identity-role)

<a id="identity-userrole"></a>
- [identity.UserRole](classification/02-core-identity-platform.md#identity-userrole)

<a id="platform-personalspace"></a>
- [platform.PersonalSpace](classification/02-core-identity-platform.md#platform-personalspace)

<a id="platform-securityinvariant"></a>
- [platform.SecurityInvariant](classification/02-core-identity-platform.md#platform-securityinvariant)

<a id="platform-module"></a>
- [platform.Module](classification/02-core-identity-platform.md#platform-module)

<a id="platform-modulerelease"></a>
- [platform.ModuleRelease](classification/02-core-identity-platform.md#platform-modulerelease)

<a id="platform-moduledependency"></a>
- [platform.ModuleDependency](classification/02-core-identity-platform.md#platform-moduledependency)

<a id="platform-modulemigration"></a>
- [platform.ModuleMigration](classification/02-core-identity-platform.md#platform-modulemigration)

<a id="platform-usermodulegrant"></a>
- [platform.UserModuleGrant](classification/02-core-identity-platform.md#platform-usermodulegrant)

<a id="platform-permission"></a>
- [platform.Permission](classification/02-core-identity-platform.md#platform-permission)

<a id="platform-adminpermission"></a>
- [platform.AdminPermission](classification/02-core-identity-platform.md#platform-adminpermission)

<a id="platform-resourcetype"></a>
- [platform.ResourceType](classification/02-core-identity-platform.md#platform-resourcetype)

<a id="platform-resource"></a>
- [platform.Resource](classification/02-core-identity-platform.md#platform-resource)

<a id="platform-resourcelink"></a>
- [platform.ResourceLink](classification/02-core-identity-platform.md#platform-resourcelink)

<a id="platform-preference"></a>
- [platform.Preference](classification/02-core-identity-platform.md#platform-preference)

<a id="identity-accountmessageintent"></a>
- [identity.AccountMessageIntent](classification/02-core-identity-platform.md#identity-accountmessageintent)

<a id="identity-mfacredential"></a>
- [identity.MfaCredential](classification/02-core-identity-platform.md#identity-mfacredential)

<a id="identity-recoverycode"></a>
- [identity.RecoveryCode](classification/02-core-identity-platform.md#identity-recoverycode)

<a id="platform-systemconnection"></a>
- [platform.SystemConnection](classification/02-core-identity-platform.md#platform-systemconnection)

<a id="security-sharelink"></a>
- [security.ShareLink](classification/03-security-sharing.md#security-sharelink)

<a id="security-sharealloweduser"></a>
- [security.ShareAllowedUser](classification/03-security-sharing.md#security-sharealloweduser)

<a id="security-supportgrant"></a>
- [security.SupportGrant](classification/03-security-sharing.md#security-supportgrant)

<a id="security-accesssession"></a>
- [security.AccessSession](classification/03-security-sharing.md#security-accesssession)

<a id="security-auditevent"></a>
- [security.AuditEvent](classification/03-security-sharing.md#security-auditevent)

<a id="security-activityevent"></a>
- [security.ActivityEvent](classification/03-security-sharing.md#security-activityevent)

<a id="files-fileobject"></a>
- [files.FileObject](classification/04-files-jobs-notifications.md#files-fileobject)

<a id="files-filereference"></a>
- [files.FileReference](classification/04-files-jobs-notifications.md#files-filereference)

<a id="files-uploadsession"></a>
- [files.UploadSession](classification/04-files-jobs-notifications.md#files-uploadsession)

<a id="notifications-notification"></a>
- [notifications.Notification](classification/04-files-jobs-notifications.md#notifications-notification)

<a id="notifications-delivery"></a>
- [notifications.Delivery](classification/04-files-jobs-notifications.md#notifications-delivery)

<a id="notifications-deliveryattempt"></a>
- [notifications.DeliveryAttempt](classification/04-files-jobs-notifications.md#notifications-deliveryattempt)

<a id="notifications-pushsubscription"></a>
- [notifications.PushSubscription](classification/04-files-jobs-notifications.md#notifications-pushsubscription)

<a id="operations-idempotency"></a>
- [operations.Idempotency](classification/04-files-jobs-notifications.md#operations-idempotency)

<a id="operations-outbox"></a>
- [operations.Outbox](classification/04-files-jobs-notifications.md#operations-outbox)

<a id="operations-inboxreceipt"></a>
- [operations.InboxReceipt](classification/04-files-jobs-notifications.md#operations-inboxreceipt)

<a id="operations-job"></a>
- [operations.Job](classification/04-files-jobs-notifications.md#operations-job)

<a id="operations-jobattempt"></a>
- [operations.JobAttempt](classification/04-files-jobs-notifications.md#operations-jobattempt)

<a id="operations-trashbatch"></a>
- [operations.TrashBatch](classification/04-files-jobs-notifications.md#operations-trashbatch)

<a id="operations-trashmember"></a>
- [operations.TrashMember](classification/04-files-jobs-notifications.md#operations-trashmember)

<a id="operations-importbatch"></a>
- [operations.ImportBatch](classification/04-files-jobs-notifications.md#operations-importbatch)

<a id="operations-importrow"></a>
- [operations.ImportRow](classification/04-files-jobs-notifications.md#operations-importrow)

<a id="operations-exportjob"></a>
- [operations.ExportJob](classification/04-files-jobs-notifications.md#operations-exportjob)

<a id="operations-backuprun"></a>
- [operations.BackupRun](classification/04-files-jobs-notifications.md#operations-backuprun)

<a id="operations-restorerun"></a>
- [operations.RestoreRun](classification/04-files-jobs-notifications.md#operations-restorerun)

<a id="operations-systemjob"></a>
- [operations.SystemJob](classification/04-files-jobs-notifications.md#operations-systemjob)

<a id="operations-resourcereminderrule"></a>
- [operations.ResourceReminderRule](classification/04-files-jobs-notifications.md#operations-resourcereminderrule)

<a id="productivity-project"></a>
- [productivity.Project](classification/05-productivity-calendar.md#productivity-project)

<a id="productivity-task"></a>
- [productivity.Task](classification/05-productivity-calendar.md#productivity-task)

<a id="productivity-taskchecklistitem"></a>
- [productivity.TaskChecklistItem](classification/05-productivity-calendar.md#productivity-taskchecklistitem)

<a id="productivity-tag"></a>
- [productivity.Tag](classification/05-productivity-calendar.md#productivity-tag)

<a id="productivity-projecttag"></a>
- [productivity.ProjectTag](classification/05-productivity-calendar.md#productivity-projecttag)

<a id="productivity-tasktag"></a>
- [productivity.TaskTag](classification/05-productivity-calendar.md#productivity-tasktag)

<a id="productivity-projectversion"></a>
- [productivity.ProjectVersion](classification/05-productivity-calendar.md#productivity-projectversion)

<a id="productivity-taskversion"></a>
- [productivity.TaskVersion](classification/05-productivity-calendar.md#productivity-taskversion)

<a id="calendar-manualevent"></a>
- [calendar.ManualEvent](classification/05-productivity-calendar.md#calendar-manualevent)

<a id="calendar-importeduid"></a>
- [calendar.ImportedUid](classification/05-productivity-calendar.md#calendar-importeduid)

<a id="calendar-reminder"></a>
- [calendar.Reminder](classification/05-productivity-calendar.md#calendar-reminder)

<a id="productivity-plannerpin"></a>
- [productivity.PlannerPin](classification/05-productivity-calendar.md#productivity-plannerpin)

<a id="productivity-goal"></a>
- [productivity.Goal](classification/05-productivity-calendar.md#productivity-goal)

<a id="productivity-goaltarget"></a>
- [productivity.GoalTarget](classification/05-productivity-calendar.md#productivity-goaltarget)

<a id="productivity-goalprogress"></a>
- [productivity.GoalProgress](classification/05-productivity-calendar.md#productivity-goalprogress)

<a id="productivity-habit"></a>
- [productivity.Habit](classification/05-productivity-calendar.md#productivity-habit)

<a id="productivity-habitschedule"></a>
- [productivity.HabitSchedule](classification/05-productivity-calendar.md#productivity-habitschedule)

<a id="productivity-habitcheckin"></a>
- [productivity.HabitCheckIn](classification/05-productivity-calendar.md#productivity-habitcheckin)

<a id="productivity-timeentry"></a>
- [productivity.TimeEntry](classification/05-productivity-calendar.md#productivity-timeentry)

<a id="productivity-focussession"></a>
- [productivity.FocusSession](classification/05-productivity-calendar.md#productivity-focussession)

<a id="productivity-focussegment"></a>
- [productivity.FocusSegment](classification/05-productivity-calendar.md#productivity-focussegment)

<a id="productivity-goaltargettask"></a>
- [productivity.GoalTargetTask](classification/05-productivity-calendar.md#productivity-goaltargettask)

<a id="productivity-timeentryversion"></a>
- [productivity.TimeEntryVersion](classification/05-productivity-calendar.md#productivity-timeentryversion)

<a id="documents-folder"></a>
- [documents.Folder](classification/06-documents-knowledge-discovery.md#documents-folder)

<a id="documents-tag"></a>
- [documents.Tag](classification/06-documents-knowledge-discovery.md#documents-tag)

<a id="documents-page"></a>
- [documents.Page](classification/06-documents-knowledge-discovery.md#documents-page)

<a id="documents-pageversion"></a>
- [documents.PageVersion](classification/06-documents-knowledge-discovery.md#documents-pageversion)

<a id="documents-archivebatch"></a>
- [documents.ArchiveBatch](classification/06-documents-knowledge-discovery.md#documents-archivebatch)

<a id="documents-archivemember"></a>
- [documents.ArchiveMember](classification/06-documents-knowledge-discovery.md#documents-archivemember)

<a id="knowledge-bookmark"></a>
- [knowledge.Bookmark](classification/06-documents-knowledge-discovery.md#knowledge-bookmark)

<a id="knowledge-snippet"></a>
- [knowledge.Snippet](classification/06-documents-knowledge-discovery.md#knowledge-snippet)

<a id="knowledge-snippetversion"></a>
- [knowledge.SnippetVersion](classification/06-documents-knowledge-discovery.md#knowledge-snippetversion)

<a id="knowledge-readingitem"></a>
- [knowledge.ReadingItem](classification/06-documents-knowledge-discovery.md#knowledge-readingitem)

<a id="organization-tag"></a>
- [organization.Tag](classification/06-documents-knowledge-discovery.md#organization-tag)

<a id="organization-resourcetag"></a>
- [organization.ResourceTag](classification/06-documents-knowledge-discovery.md#organization-resourcetag)

<a id="organization-collection"></a>
- [organization.Collection](classification/06-documents-knowledge-discovery.md#organization-collection)

<a id="organization-collectionmember"></a>
- [organization.CollectionMember](classification/06-documents-knowledge-discovery.md#organization-collectionmember)

<a id="organization-template"></a>
- [organization.Template](classification/06-documents-knowledge-discovery.md#organization-template)

<a id="discovery-searchprojection"></a>
- [discovery.SearchProjection](classification/06-documents-knowledge-discovery.md#discovery-searchprojection)

<a id="discovery-savedquery"></a>
- [discovery.SavedQuery](classification/06-documents-knowledge-discovery.md#discovery-savedquery)

<a id="discovery-favorite"></a>
- [discovery.Favorite](classification/06-documents-knowledge-discovery.md#discovery-favorite)

<a id="discovery-recentitem"></a>
- [discovery.RecentItem](classification/06-documents-knowledge-discovery.md#discovery-recentitem)

<a id="discovery-dashboardwidget"></a>
- [discovery.DashboardWidget](classification/06-documents-knowledge-discovery.md#discovery-dashboardwidget)

<a id="discovery-dashboard"></a>
- [discovery.Dashboard](classification/06-documents-knowledge-discovery.md#discovery-dashboard)

<a id="finance-account"></a>
- [finance.Account](classification/07-finance-vault.md#finance-account)

<a id="finance-category"></a>
- [finance.Category](classification/07-finance-vault.md#finance-category)

<a id="finance-transaction"></a>
- [finance.Transaction](classification/07-finance-vault.md#finance-transaction)

<a id="finance-transactionleg"></a>
- [finance.TransactionLeg](classification/07-finance-vault.md#finance-transactionleg)

<a id="finance-transactionsplit"></a>
- [finance.TransactionSplit](classification/07-finance-vault.md#finance-transactionsplit)

<a id="finance-transactionversion"></a>
- [finance.TransactionVersion](classification/07-finance-vault.md#finance-transactionversion)

<a id="finance-bill"></a>
- [finance.Bill](classification/07-finance-vault.md#finance-bill)

<a id="finance-paymentallocation"></a>
- [finance.PaymentAllocation](classification/07-finance-vault.md#finance-paymentallocation)

<a id="finance-recurringrule"></a>
- [finance.RecurringRule](classification/07-finance-vault.md#finance-recurringrule)

<a id="finance-subscription"></a>
- [finance.Subscription](classification/07-finance-vault.md#finance-subscription)

<a id="finance-subscriptionprice"></a>
- [finance.SubscriptionPrice](classification/07-finance-vault.md#finance-subscriptionprice)

<a id="finance-budget"></a>
- [finance.Budget](classification/07-finance-vault.md#finance-budget)

<a id="finance-budgetline"></a>
- [finance.BudgetLine](classification/07-finance-vault.md#finance-budgetline)

<a id="finance-savingsgoal"></a>
- [finance.SavingsGoal](classification/07-finance-vault.md#finance-savingsgoal)

<a id="finance-savingsaccount"></a>
- [finance.SavingsAccount](classification/07-finance-vault.md#finance-savingsaccount)

<a id="finance-debt"></a>
- [finance.Debt](classification/07-finance-vault.md#finance-debt)

<a id="vault-item"></a>
- [vault.Item](classification/07-finance-vault.md#vault-item)

<a id="vault-itemversion"></a>
- [vault.ItemVersion](classification/07-finance-vault.md#vault-itemversion)

<a id="vault-keyenvelope"></a>
- [vault.KeyEnvelope](classification/07-finance-vault.md#vault-keyenvelope)

<a id="vault-rotationrun"></a>
- [vault.RotationRun](classification/07-finance-vault.md#vault-rotationrun)

<a id="news-feed"></a>
- [news.Feed](classification/08-news-shopping-developer.md#news-feed)

<a id="news-feedsubscription"></a>
- [news.FeedSubscription](classification/08-news-shopping-developer.md#news-feedsubscription)

<a id="news-article"></a>
- [news.Article](classification/08-news-shopping-developer.md#news-article)

<a id="news-articlestate"></a>
- [news.ArticleState](classification/08-news-shopping-developer.md#news-articlestate)

<a id="news-topicwatch"></a>
- [news.TopicWatch](classification/08-news-shopping-developer.md#news-topicwatch)

<a id="news-topicmatch"></a>
- [news.TopicMatch](classification/08-news-shopping-developer.md#news-topicmatch)

<a id="shopping-trackedproduct"></a>
- [shopping.TrackedProduct](classification/08-news-shopping-developer.md#shopping-trackedproduct)

<a id="shopping-priceobservation"></a>
- [shopping.PriceObservation](classification/08-news-shopping-developer.md#shopping-priceobservation)

<a id="shopping-pricealertrule"></a>
- [shopping.PriceAlertRule](classification/08-news-shopping-developer.md#shopping-pricealertrule)

<a id="shopping-pricealertevent"></a>
- [shopping.PriceAlertEvent](classification/08-news-shopping-developer.md#shopping-pricealertevent)

<a id="shopping-wishlistitem"></a>
- [shopping.WishlistItem](classification/08-news-shopping-developer.md#shopping-wishlistitem)

<a id="shopping-comparison"></a>
- [shopping.Comparison](classification/08-news-shopping-developer.md#shopping-comparison)

<a id="shopping-comparisonitem"></a>
- [shopping.ComparisonItem](classification/08-news-shopping-developer.md#shopping-comparisonitem)

<a id="shopping-seller"></a>
- [shopping.Seller](classification/08-news-shopping-developer.md#shopping-seller)

<a id="shopping-purchaseorder"></a>
- [shopping.PurchaseOrder](classification/08-news-shopping-developer.md#shopping-purchaseorder)

<a id="shopping-orderline"></a>
- [shopping.OrderLine](classification/08-news-shopping-developer.md#shopping-orderline)

<a id="shopping-returnrecord"></a>
- [shopping.ReturnRecord](classification/08-news-shopping-developer.md#shopping-returnrecord)

<a id="shopping-warranty"></a>
- [shopping.Warranty](classification/08-news-shopping-developer.md#shopping-warranty)

<a id="developer-tooldefinition"></a>
- [developer.ToolDefinition](classification/08-news-shopping-developer.md#developer-tooldefinition)

<a id="developer-toolfavorite"></a>
- [developer.ToolFavorite](classification/08-news-shopping-developer.md#developer-toolfavorite)

<a id="developer-toolhistory"></a>
- [developer.ToolHistory](classification/08-news-shopping-developer.md#developer-toolhistory)

<a id="developer-repository"></a>
- [developer.Repository](classification/08-news-shopping-developer.md#developer-repository)

<a id="developer-rankingsnapshot"></a>
- [developer.RankingSnapshot](classification/08-news-shopping-developer.md#developer-rankingsnapshot)

<a id="developer-rankingentry"></a>
- [developer.RankingEntry](classification/08-news-shopping-developer.md#developer-rankingentry)

<a id="developer-savedrepository"></a>
- [developer.SavedRepository](classification/08-news-shopping-developer.md#developer-savedrepository)

<a id="automation-definition"></a>
- [automation.Definition](classification/09-automation-monitoring.md#automation-definition)

<a id="automation-definitionversion"></a>
- [automation.DefinitionVersion](classification/09-automation-monitoring.md#automation-definitionversion)

<a id="automation-schedule"></a>
- [automation.Schedule](classification/09-automation-monitoring.md#automation-schedule)

<a id="automation-run"></a>
- [automation.Run](classification/09-automation-monitoring.md#automation-run)

<a id="automation-steprun"></a>
- [automation.StepRun](classification/09-automation-monitoring.md#automation-steprun)

<a id="automation-connection"></a>
- [automation.Connection](classification/09-automation-monitoring.md#automation-connection)

<a id="automation-webhook"></a>
- [automation.Webhook](classification/09-automation-monitoring.md#automation-webhook)

<a id="automation-webhookdelivery"></a>
- [automation.WebhookDelivery](classification/09-automation-monitoring.md#automation-webhookdelivery)

<a id="monitoring-monitor"></a>
- [monitoring.Monitor](classification/09-automation-monitoring.md#monitoring-monitor)

<a id="monitoring-observation"></a>
- [monitoring.Observation](classification/09-automation-monitoring.md#monitoring-observation)

<a id="monitoring-incident"></a>
- [monitoring.Incident](classification/09-automation-monitoring.md#monitoring-incident)

<a id="assets-personalasset"></a>
- [assets.PersonalAsset](classification/10-assets-career-learning.md#assets-personalasset)

<a id="assets-assetaccessory"></a>
- [assets.AssetAccessory](classification/10-assets-career-learning.md#assets-assetaccessory)

<a id="assets-assetwarranty"></a>
- [assets.AssetWarranty](classification/10-assets-career-learning.md#assets-assetwarranty)

<a id="assets-repair"></a>
- [assets.Repair](classification/10-assets-career-learning.md#assets-repair)

<a id="assets-assetloan"></a>
- [assets.AssetLoan](classification/10-assets-career-learning.md#assets-assetloan)

<a id="assets-assetversion"></a>
- [assets.AssetVersion](classification/10-assets-career-learning.md#assets-assetversion)

<a id="assets-digitalasset"></a>
- [assets.DigitalAsset](classification/10-assets-career-learning.md#assets-digitalasset)

<a id="assets-domaindetail"></a>
- [assets.DomainDetail](classification/10-assets-career-learning.md#assets-domaindetail)

<a id="assets-hostingdetail"></a>
- [assets.HostingDetail](classification/10-assets-career-learning.md#assets-hostingdetail)

<a id="assets-vpsdetail"></a>
- [assets.VpsDetail](classification/10-assets-career-learning.md#assets-vpsdetail)

<a id="assets-certificatedetail"></a>
- [assets.CertificateDetail](classification/10-assets-career-learning.md#assets-certificatedetail)

<a id="assets-licensedetail"></a>
- [assets.LicenseDetail](classification/10-assets-career-learning.md#assets-licensedetail)

<a id="assets-servicedetail"></a>
- [assets.ServiceDetail](classification/10-assets-career-learning.md#assets-servicedetail)

<a id="assets-renewalrecord"></a>
- [assets.RenewalRecord](classification/10-assets-career-learning.md#assets-renewalrecord)

<a id="career-company"></a>
- [career.Company](classification/10-assets-career-learning.md#career-company)

<a id="career-jobapplication"></a>
- [career.JobApplication](classification/10-assets-career-learning.md#career-jobapplication)

<a id="career-interview"></a>
- [career.Interview](classification/10-assets-career-learning.md#career-interview)

<a id="career-applicationevent"></a>
- [career.ApplicationEvent](classification/10-assets-career-learning.md#career-applicationevent)

<a id="career-resume"></a>
- [career.Resume](classification/10-assets-career-learning.md#career-resume)

<a id="career-resumeversion"></a>
- [career.ResumeVersion](classification/10-assets-career-learning.md#career-resumeversion)

<a id="career-resumeshareversion"></a>
- [career.ResumeShareVersion](classification/10-assets-career-learning.md#career-resumeshareversion)

<a id="learning-skill"></a>
- [learning.Skill](classification/10-assets-career-learning.md#learning-skill)

<a id="learning-skillevidence"></a>
- [learning.SkillEvidence](classification/10-assets-career-learning.md#learning-skillevidence)

<a id="learning-course"></a>
- [learning.Course](classification/10-assets-career-learning.md#learning-course)

<a id="learning-coursemilestone"></a>
- [learning.CourseMilestone](classification/10-assets-career-learning.md#learning-coursemilestone)

<a id="learning-certification"></a>
- [learning.Certification](classification/10-assets-career-learning.md#learning-certification)

<a id="learning-plan"></a>
- [learning.Plan](classification/10-assets-career-learning.md#learning-plan)

<a id="learning-planitem"></a>
- [learning.PlanItem](classification/10-assets-career-learning.md#learning-planitem)

<a id="learning-worklog"></a>
- [learning.WorkLog](classification/10-assets-career-learning.md#learning-worklog)

<a id="learning-certificationversion"></a>
- [learning.CertificationVersion](classification/10-assets-career-learning.md#learning-certificationversion)

<a id="assets-digitalassetversion"></a>
- [assets.DigitalAssetVersion](classification/10-assets-career-learning.md#assets-digitalassetversion)

<a id="assets-component"></a>
- [assets.Component](classification/10-assets-career-learning.md#assets-component)
