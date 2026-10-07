# Student 4 commit scope

This change contains personal execution evidence and AI-assisted extensions by IT24100966, Kaluarachchige P.E.K.

- Personal additions: the safety input (8.49, 31, false, true) and the Flutter expired-session zero-request test.
- Supporting shared work: the safety boundary theory and its five earlier cases, and the nurse completion/reassignment extension in the existing backend concurrency test. These are included to reproduce the recorded tests, not claimed as personally authored.
- Results: 4 Flutter, 2 backend and 8 safety cases passed in the recorded personal runs. Earlier results are retained, not added to this total.
- Excluded: group security probes, consultation tests, planner changes, web accessibility work, performance runners and the group report. They remain local.
- Evidence contains synthetic test data and machine-local paths. No local application secrets are included. Logs/TRX are intentionally tracked evidence.
- The report section and PDF describe historical runs. A Git commit created afterward is a packaging reference, not the original run-time commit.

Run the commands recorded in CONTRIBUTION_RECORD.md from the project root. Backend tests require CAREPULSE_TEST_CONNECTION pointing to an isolated PostgreSQL server; they create and delete temporary databases. Flutter tests run from mobile-app. No Neon or live Gemini connection is required for these selected tests.
