# Database upgrade and preservation

> **29 September 2026: your configured Neon database has now been upgraded successfully. No manual migration is needed for that same database.** See [NEON_UPGRADE_STATUS.md](NEON_UPGRADE_STATUS.md). The procedure below remains a reference for other databases and future upgrades.

## What changed

`20260928050315_IntegratedWorkflowSafety` adds linked workflow state, nullable confirmed coordinates, dispatch escalation/arrival fields, location timestamps, concurrency mapping, foreign keys, and uniqueness constraints. Scheduling tables gain the missing soft-delete audit columns. There is now one model snapshot: `backend-api/Data/Migrations/CarePulseDbContextModelSnapshot.cs`.

Two historical migration bodies were repaired to make a fresh checkout reproducible: the patient migration no longer recreates Identity tables already made by the dispatch baseline, and the September 21 scheduling migration defers doctor foreign keys until DoctorProfiles exists. Their migration IDs remain unchanged. EF will not rerun an already-recorded migration; the new migration conditionally adds the deferred doctor foreign keys. Both a complete empty-database replay and model/snapshot consistency are tested. This does **not** establish that every manually altered shared database is compatible.

## New local installation

Follow README. Apply the entire history to a new `carepulse_dev` database. Never generate a replacement Initial migration or use `EnsureCreated` for the application database. `EnsureCreated` is used only by isolated test fixtures; a separate test verifies the actual migrations.

## Existing Neon/shared installation

1. Preserve records. Create a provider backup/branch and test the upgrade there first. Record the source commit, migration history and backup/restore procedure. Stop concurrent schema deployments.
2. Inspect `__EFMigrationsHistory`. The preflight script expects every migration through `20260927104724_AddStudent2TriageIntegration`; if any are absent, compare actual tables/columns before proceeding. Do not insert fake history rows.
3. Run `scripts/migration-preflight.sql` read-only against the copy. All issue counts must be zero. It reports counts/IDs, not patient details. Resolve each orphan, duplicate or unsupported legacy status against an authoritative record. No repair script deletes or invents patient/staff IDs.
4. Existing active dispatches need staff-confirmed destinations, which were not previously stored reliably. Finish/reconcile those cases under the old version, or agree a reviewed data migration before switching the application. Do not invent zero coordinates. Legacy `Escalated` needs review because the new design stores escalation separately from route status.
5. Generate and review SQL from the actual last applied migration:

   ```sh
   dotnet ef migrations script 20260927104724_AddStudent2TriageIntegration 20260928050315_IntegratedWorkflowSafety --project backend-api --context CarePulseDbContext --output /tmp/carepulse-upgrade.sql
   ```

   Use another temporary output path on Windows. The example assumes the listed starting migration really is applied. For differing histories, review an idempotent full script (`--idempotent`) and the database schema together.
6. Point `ConnectionStrings__DefaultConnection` at the isolated branch, apply the reviewed upgrade, and perform the manual cross-client workflow. Check actual row relationships and nurse availability through a new connection.
7. Only after those checks, back up the shared environment, schedule the schema/application rollout and apply the same reviewed script. Keep the backup until acceptance is complete. A schema rollback can remove new data; prefer a reviewed forward fix or restoring the backup.

The initial audit did not change Neon. The later authorized upgrade is documented in [NEON_UPGRADE_STATUS.md](NEON_UPGRADE_STATUS.md); records were preserved in live tables or a private archive.

## Uploaded files and secrets

New uploads are private in `App_Data/uploads`. Old files in `wwwroot/uploads` remain on the original laptop; the protected download service copies them privately after authorization. Static serving is disabled. Removed Git tracking does not copy existing files to teammates or erase prior Git history: transfer legitimate required test files through controlled storage, or upload new synthetic files.

A hardcoded database connection was removed from `CheckDb.cs`, which was an obsolete, uncompiled diagnostic. Committed API/JWT/database configuration is now blank and the original machine configuration is retained only in ignored `appsettings.Local.json`. **Rotate credentials that were previously committed**, including database/JWT/Gemini credentials where applicable, and replace local/deployed values. Removing current source text does not revoke credentials or erase history. Do not commit private dumps, generated production scripts containing data, uploaded patient files or `.Local.json`.

## References

Microsoft recommends reviewing generated migration SQL before deployment: [Applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying). The repaired historical bodies address this repository's broken merge history; they are not a general recommendation to rewrite deployed migrations.
