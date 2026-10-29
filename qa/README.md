# QA evidence and repository policy

Run reports, case/matrix CSVs, sanitized JSON evidence and executable QA harnesses
are versioned under `runs/<test-run-id>/`. A report describes its original source
fingerprint and does not certify a later merge, commit or build.

Build outputs, package caches, machine-specific backups, raw runner XML/TRX/logs
and Playwright output stay local and are ignored. Report references to these
files identify local evidence; they are not promised as remote attachments.
Screenshots from the October 18 QA run were inspected internally and deleted.

Never commit runtime credentials, environment secrets, storage content, browser
authentication state or unredacted traces. Use the official isolated runtime and
its ownership-checked cleanup. Do not run the historical harnesses against a
developer or production database: their captured runtime paths are run-specific.
