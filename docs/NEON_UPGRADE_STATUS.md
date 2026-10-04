# Neon upgrade completed — 29 September 2026

**Your configured shared Neon database is ready for the updated backend. You do not need to run the upgrade yourself.** Restart the backend, web app and Flutter app, then follow [App-only manual testing](APP_MANUAL_TEST_PLAN.md).

## Completed

- Saved a private full backup and successfully restored it into an isolated local database.
- Rehearsed archival and migration on that restored copy.
- Adjusted the migration to preserve existing scheduling audit columns already present in Neon. Fresh installations still work; rollback retains these potentially pre-existing audit columns.
- Applied the reviewed repair and `20260928050315_IntegratedWorkflowSafety` migration to Neon in one transaction, after the owner authorized archiving obsolete test records.
- Verified every original row across all public application tables against the remaining live data plus archive before commit. Migration history was excluded because the new migration adds one row.
- Rechecked all 14 integrity conditions: every issue count is zero.
- Read all 27 mapped entity types through the updated EF Core model in a read-only Neon transaction. No pending migrations.
- Backend suite: **109 tests passed** (107 API/database tests and 2 safety tests).

## Preserved and archived data

Patient profiles, staff accounts, medical vault records and valid application records were preserved. One nurse was released from an archived invalid active dispatch.

The private Neon schema `carepulse_archive_20260929` stores original rows as JSON, with their source table, ID, reason and archive time. Public schema access was revoked. It is not an application screen and is not deleted by the application.

| Original table | Archived rows |
| --- | ---: |
| TriageTickets | 7 |
| AppointmentSlots | 2 |
| DispatchTickets | 3 |
| OnSiteVitalsRecords | 5 |
| RouteLogs | 7 |
| RiskAssessments | 7 |
| ApprovalQueues | 6 |
| AiTriageLogs | 13 |
| NurseProfiles | 1 pre-update snapshot; the nurse remains active |

Total: 51 preserved archive rows. The third dispatch also belonged to an archived missing-patient triage case. All five vitals rows belonged to archived dispatches, so none needed to be chosen as the current clinical measurement. There are no remaining active dispatches from those obsolete tests; create a fresh triage to test dispatch.

## Recovery artifacts — private, not committed

- Full pre-upgrade backup: `backend-api/App_Data/backups/neon-before-upgrade-20260929.dump`
- Exact executed SQL: `backend-api/App_Data/backups/neon-applied-upgrade-20260929.sql`
- Executed SQL SHA-256: `55b877052e792b252a3f49fc79cf961709ba0da5547814baccfcdc9f88eb5c3b`

The backup directory is private and ignored by Git. Keep it until the team finishes acceptance testing. If recovery is needed, restore into a separate database/Neon branch first; restoring over the shared database would overwrite later work.

The dated repair source in `scripts/neon-test-data-repair-20260929.sql` documents the one-time archival selection. **Do not rerun it.** It is not part of ordinary setup or migrations. Teammates using this same Neon database need only pull the updated code and configure their local connection; a new independent database still needs the normal migrations.

No real Gemini call or device test was part of this database upgrade. Those remain in the app-only checklist.
