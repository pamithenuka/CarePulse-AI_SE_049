# Individual testing contribution record

Status: Student 4 has supplied successful personal Flutter, backend dispatch and safety test runs (6 October 2026). Other members' personal runs remain unconfirmed. The existing group tests and reports were prepared and executed with Codex assistance.

| Student ID | Name | Project/test area | Personal testing status |
| --- | --- | --- | --- |
| IT24101521 | Bandara EMGO | Confirm | Not yet completed |
| IT24100966 | Kaluarachchige P.E.K | Student 4 — dispatch | Extended Flutter: 4 passed; backend dispatch: 2 passed; extended safety: 8 passed; explanation and declaration pending |
| IT24100009 | Walpala M.W.H.T | Confirm | Not yet completed |
| IT24101037 | Savindu | Confirm | Not yet completed |

Copy the following short record once per member:

- Name and ID:
- Project area (Student 1/2/3/4):
- Date and source commit:
- Test file and exact command:
- Why this test matters:
- Expected result:
- Actual result (passed/failed/skipped counts):
- Evidence file/screenshot path:
- What I personally wrote, changed or verified:
- What assistance I used, including AI:
- My commit/diff link, if I changed anything:
- Failure and retest, if any:
- One limitation I can explain:

Do not claim authorship of an existing test just because you ran it. If the lecturer expects personally implemented tests, adapt or extend a meaningful case yourself, explain the assertions and keep your actual change history. Use INDIVIDUAL_TESTING_GUIDE.md for suggested starting points.

## Student 4 — personal Flutter test run

- Date: 6 October 2026.
- Test file: `mobile-app/test/services/dispatch_session_test.dart`.
- Commands (from `mobile-app`): `flutter pub get`, then `flutter test test/services/dispatch_session_test.dart`.
- Expected: three tests pass, checking expired session rejection, location-update HTTP 401 propagation, and completion HTTP 409 propagation.
- Actual: user supplied terminal output ending `00:01 +3: All tests passed!` (3 passed).
- Evidence: initial terminal output pasted by the student, followed by a personal rerun with `--reporter expanded` saved in [results.log](evidence/personal/student4-flutter-run1/results.log). Codex inspected the saved log: all three named tests passed. Exact source commit at execution was not captured.
- Personal contribution: ran the existing tests and supplied the result. No personal test authorship or modification claimed.
- Assistance: Codex selected/explained the command and documented the supplied result; tests were previously prepared with assistance.
- Limitation: simulated HTTP/session responses; this does not test real GPS, Render, Neon, or live Gemini.
- Still pending: explanation/practice and required AI declaration.

## Student 4 — personal backend dispatch run

- Date: 6 October 2026.
- Setup: student created `carepulse-qm-viva` using `postgres:18`, published on localhost port 55442, and confirmed `accepting connections`.
- Command from project root: `dotnet test tests/CarePulse.Api.Tests --configuration Release --filter "FullyQualifiedName~GoldenWorkflow_ExecutesAllAgents|FullyQualifiedName~ConcurrentAssignments_ClaimNurseOnce" --logger trx --results-directory docs/qm/evidence/personal/student4-run1`.
- Expected and actual: 2 passed, 0 failed, 0 skipped.
- Evidence: [saved TRX](evidence/personal/student4-run1/_192_2026-10-06_15_44_29.trx) and student-supplied terminal output.
- Scope: approval/ownership/durable completion and concurrent nurse assignment followed by release/reassignment. Local test database and substituted AI; no deployed-system or live-model claim.
- Build warning: EF1002 in MigrationTests.cs concerns interpolation of three fixed table names. That migration test was not selected; warning did not fail the dispatch tests.
- Personal contribution: executed existing tests and supplied results, with Codex guidance. No new test authorship claimed. Exact source commit at execution was not captured.

## Student 4 — personal safety test run

- Date: 6 October 2026.
- Command from project root: `dotnet test tests/CarePulse.Ai.GoldenTests --configuration Release --filter "FullyQualifiedName~ValidationAgentGoldenTests" --logger trx --results-directory docs/qm/evidence/personal/student4-safety-run1`.
- Expected and actual: 7 passed, 0 failed, 0 skipped. Saved TRX counters verified by Codex.
- Evidence: [saved TRX](evidence/personal/student4-safety-run1/_192_2026-10-06_15_49_13.trx) and student-supplied terminal output.
- Scope: five severity/ETA boundary cases and two missing-provider-configuration fallback cases; human approval required throughout. In-memory database, no live Gemini calls.
- Personal contribution: executed existing tests and supplied results with Codex guidance; no test modification or independent authorship claimed yet.
- Exact source commit at execution was not captured. Personal explanation/practice and AI declaration remain pending.

## Student 4 — personal safety test extension and rerun

- Date: 6 October 2026.
- Student added `[InlineData(8.49, 31, false, true)]` to `ValidationAgentGoldenTests.cs` following a Codex suggestion; source and the student's screenshot confirm the added case.
- Purpose: check that ETA above 30 triggers its warning independently of severity below 8.5. Existing assertions also check human approval and the exact number of flags.
- Command: `dotnet test tests/CarePulse.Ai.GoldenTests --configuration Release --filter "FullyQualifiedName~ValidationAgentGoldenTests" --logger trx --results-directory docs/qm/evidence/personal/student4-safety-run2`.
- Actual result: 8 passed, 0 failed, 0 skipped; saved TRX counters verified by Codex.
- Evidence: [saved TRX](evidence/personal/student4-safety-run2/_192_2026-10-06_15_54_30.trx).
- Contribution: student applied and ran an AI-assisted parameterized test extension, not independent authorship of the suite. The student's own explanation is still pending.
- This rerun replaces the previous 7-case safety result for the current source; it is not eight additional distinct tests. Across the three selected suites, latest results cover 13 distinct cases (3 Flutter, 2 backend, 8 safety).
- No failed product test occurred in these personal runs; do not invent a failure/retest defect.

## Student 4 — personal Flutter test extension

- Date: 6 October 2026.
- Student applied the Codex-provided test `expired session prevents a location request from being sent` in `mobile-app/test/services/dispatch_session_test.dart`.
- It calls the actual `ApiService.updateLocation` with an expired mocked session, asserts a 401 exception, and checks zero requests reached the mock HTTP client. This extends the original token-only check to the service boundary.
- Student ran `flutter test test/services/dispatch_session_test.dart --reporter expanded` from `mobile-app`, saving output in [extended-results.log](evidence/personal/student4-flutter-run1/extended-results.log).
- Actual: 4 passed; source and saved output inspected by Codex. This replaces the earlier three-case Flutter result for the extended file.
- Latest selected-suite results: 14 distinct cases (4 Flutter, 2 backend, 8 safety), from separate runs, not a fresh full-project run.
- Attribution: AI-generated test applied and executed by the student with guidance. Independent design/understanding has not yet been demonstrated. No real network, GPS or deployed-system coverage claimed.

## Student 4 — personal verification of tracking defect fix

- Date: 8 October 2026. Defect: S4-DEF-01 (old location responses affecting paused/restarted tracking).
- Codex reproduced two failing regression cases, implemented the fix and verified it. Student then personally ran `flutter test test/services/dispatch_tracking_test.dart test/services/dispatch_session_test.dart --reporter expanded` from mobile-app.
- Actual: 7 passed, confirmed by student-pasted terminal output ending `00:00 +7: All tests passed!`. A subsequent personal rerun was saved in [student4-tracking-personal.log](evidence/personal/student4-tracking-personal.log); Codex inspected the file and confirmed 7 passed. The separate before/after logs belong to Codex's runs.
- Attribution: personal verification of an AI-assisted fix; not independent discovery or test authorship.
- Coverage count across historical personal runs is now 17 distinct cases (7 Flutter including 3 new tracking cases, 2 backend, 8 safety). Backend and safety results remain from 6 October, not freshly rerun against this fix. Do not count the overlapping four session cases twice.
- See student4-handover/DISPATCH_TRACKING_DEFECT.md for cause, fix, evidence and report wording. Original 6 October no-defect statements remain historical; the additional review found one real product defect. Fix and new evidence have not been committed/deployed by Codex.

## AI declaration

The exact module CLEAR instructions are still pending. Obtain them from the lecturer, then write the required declaration using actual use and verification. Do not invent the framework's wording or a student's reflection.
