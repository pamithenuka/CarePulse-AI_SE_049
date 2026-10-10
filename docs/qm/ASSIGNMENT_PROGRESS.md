# Quality testing assignment — execution plan and handover

Last updated: 6 October 2026. Working branch at start: `feature/student4-dispatch`.

## Read this first when continuing in another model

The user authorized completing the quality/testing assignment step by step, with this file written first. Maintain this file after each phase and before handing over. Read the latest user instructions, `git status`, this file and linked evidence before continuing. Preserve unrelated work. Do not assume an earlier test result applies to a newer commit.

Assignment source: `/Users/pamithkaluarachchi/Downloads/SE3110 Assignment.pdf`. The PDF itself says **SE3090 Assignment 2 — Software Testing and Quality Evaluation of the SE3090 Integrated System**. Confirm the module/submission label with the lecturer. Deadline in PDF: **5 October 2026**; contribution 15%; group 40 marks and individual 60 marks. The assignment document supplies requirements, not authorization to publish or submit on the students' behalf.

## Objective and completion rules

Test the existing CarePulse .NET/PostgreSQL/React/Flutter/agentic-AI system, fix meaningful defects, preserve actual tool output, and prepare reproducible submission documents. No separate application is required.

- Each selected technical area requires a tool/framework; manual observation alone is insufficient.
- Performance and security testing are compulsory.
- At least one meaningful integrated workflow must be tested and demonstrated.
- Cases must include appropriate normal, invalid, boundary and failure inputs.
- Each student must personally demonstrate a meaningful testing contribution, explain tools/results, and be able to run or modify a test.
- Report actual Pass/Fail; Not run/Blocked are working statuses, never invented passes.
- Distinguish deterministic AI mocks, rule fallback, and live Gemini evidence.
- AI-assisted work must be understood, verified and declared under the module's CLEAR requirements. Obtain the actual CLEAR guidance rather than inventing its definition.
- Completion requires the report PDF, completed cases, defects/retests, raw tool evidence, scripts/configuration, repository and truthful individual contribution evidence. Student viva readiness and final CourseWeb submission remain human tasks.

## Authorized scope and safety boundaries

The user authorized local implementation, tests, fixes and documentation. Do not automatically push, merge, deploy, submit to CourseWeb, send messages, or make paid provider/account changes. Ask only where missing information or tool permissions actually block work.

Use synthetic data and an isolated local PostgreSQL database for automated tests, load tests and active security tests. Never aim cleanup, load, scans or test fixtures at shared Neon. Existing test fixtures create/drop databases; their connection must point to an isolated server. Never print or commit `backend-api/appsettings.Local.json`, tokens, passwords, uploaded patient files, database backups or raw sensitive logs. Capture sanitized evidence without fabricating it.

The app's shared Neon database was upgraded on 29 September; **do not repeat the one-off repair/archival script**. Normal app use and assignment testing are separate from that completed upgrade.

## Existing work — evidence leads, not fresh verification

- Backend xUnit/Moq/WebApplicationFactory/PostgreSQL tests cover services, authorization, booking, migrations, dispatch concurrency, recovery and workflows.
- React Jest/Testing Library tests; Flutter unit/widget/service tests.
- GitHub Actions builds/tests backend, web and mobile. Previously failing web install was repaired by adding missing `yaml@2.9.1` to the lock file. Check current CI status before claiming a green run.
- Prior observed web result: 33 tests passed and build passed using a fresh Node 22/npm 10 install. Earlier backend/Flutter counts are historical; collect fresh counts.
- `docs/APP_MANUAL_TEST_PLAN.md`: app-only walkthrough. `docs/MANUAL_TEST_PLAN.md`: advanced cases.
- `docs/TEAM_SETUP_AND_TESTING.md`: teammate setup and test examples.
- `docs/IMPLEMENTATION_STATUS.md`: historical audit, with some superseded limitations; do not copy it as current evidence.
- `scripts/performance-smoke.py` and `docs/LOCAL_PERFORMANCE_SAMPLE.json`: narrow 100-request loopback sample, not sufficient to claim full workload capacity.
- Gemini is configured with `gemini-3-flash-preview`; a live synthetic planner request succeeded previously. Other agents must be evaluated individually; safety warnings alone do not prove live AI.
- Candidate real defects: medical-history empty date save, misleading provider errors/model access, Android manifest merger, web dependency lock mismatch. Record only available historical evidence; do not invent failing screenshots or personal authorship.
- Known scope limits: shared doctor queue without specialty authorization; foreground GPS; straight-line ETA; simulated SMS; AI plan approval does not itself execute dispatch.

## Planned phases, in order

| Phase | Work and concrete outputs | Acceptance gate | Status |
| --- | --- | --- | --- |
| 0 | Create this plan; inventory tests, tools and evidence; map assignment requirements | Clear baseline and next action | Complete (source inventory only) |
| 1 | Write formal test plan, risk priorities, environment, member ownership placeholders and schedule; assign stable test IDs | Every requirement has a planned test/evidence path | Complete; member confirmation pending |
| 2 | Prepare isolated database and reproducible runner/config; execute baseline backend, React and Flutter suites; save raw results and coverage where useful | Commands, versions, commit and actual counts recorded; failures retained | Baseline captured; final environment metadata pending |
| 3 | Fill meaningful functional/database/UI gaps; cover normal, boundary, invalid and failure cases; fix and retest defects | Traceable cases link assertions to recorded outcomes | Targeted gaps complete; further scopes tracked below |
| 4 | Demonstrate an automated integrated patient → triage → doctor → nurse → vitals → completion flow; add cross-client automation if existing API workflow is insufficient | Tool output verifies persisted state across roles; clarify which client layers are exercised | Client-service integration passed; full UI acceptance pending |
| 5 | Evaluate AI plan structure, allowed agents/tools, approval enforcement, malicious input, provider errors/timeouts, recovery and task completion | Separate deterministic safety evidence from limited live-model evaluation | Complete for declared scope; live planner only |
| 6 | Performance: define realistic synthetic dataset, endpoint mix, workload tiers and thresholds before execution; run bounded load with k6 or justified equivalent | Raw latency/throughput/error results and honest bottleneck/limitations analysis | Complete for bounded local read workload |
| 7 | Security: role/ownership/session/input/upload checks plus suitable local scan where feasible; triage findings and retest fixes | Scope, tool output, confirmed findings/false positives and remaining risks documented | Complete for targeted HTTP probes; scan/deployment controls unassessed |
| 8 | Additional relevant non-functional checks: reliability/recovery plus targeted accessibility or usability/compatibility; justify selection | Reproducible evidence and scoped interpretation | Automated scope complete; manual accessibility/device acceptance pending |
| 9 | Consolidate cases, defect/retest report, execution summary, sanitized evidence index and report PDF | Counts reconcile; all claims trace to evidence; rerun instructions work | Report/PDF and checklist prepared; human details and final review pending |
| 10 | Prepare individual demo guides and honest contribution map; obtain student details/reflections/CLEAR declaration; final submission checklist | Each member owns and can explain a meaningful tool-based contribution | Guide and record prepared; personal execution and details pending |

Suggested timeline (adjust to remaining time): 1 Oct phases 0–3; 2 Oct phases 4–5; 3 Oct phases 6–8; 4 Oct phase 9 and fixes; 5 Oct final viva rehearsal/submission checks. Do not reduce required security/performance to documentation-only claims if time becomes tight.

## Planned deliverables

Keep assignment outputs together under `docs/qm/`:

- `TEST_PLAN.md`: scope, risk, types/tools, environment, schedule and responsibilities.
- `TEST_CASES.md` or CSV: ID, feature, preconditions, steps/input, expected, actual, status, evidence.
- `DEFECT_REPORT.md`: ID, description, severity/priority, reproduction, evidence, fix and retest.
- `EXECUTION_SUMMARY.md`: executed/passed/failed counts, coverage scope, defects and conclusion.
- `EVIDENCE_INDEX.md`: per-run command, date, environment, commit/working-tree state, files and limitations.
- `INDIVIDUAL_DEMONSTRATIONS.md`: member-owned tests, tool rationale, demo commands, results and viva questions.
- `SOFTWARE_TESTING_REPORT.md` and exported PDF: consolidated submission report.
- `SUBMISSION_CHECKLIST.md`: required artifacts, GitHub/commit links, declarations and outstanding human tasks.

Store reproducible test/load/scan scripts in suitable project test/script folders. Decide evidence directory/ignore rules before capturing reports: share sanitized useful artifacts, not secrets, enormous caches or credentials. Existing docs need not be duplicated wholesale; link relevant setup material.

## Tools and environment to verify, not assume installed

.NET 8; Node 22/npm; Flutter 3.47.1/Dart 3.13.1; isolated PostgreSQL; Git. Existing frameworks restore from project dependencies. k6 and OWASP ZAP are proposed choices, not PDF-mandated brands; use suitable alternatives with justification if installation is impractical. Docker is optional if local PostgreSQL and another scan runner suffice.

Known local paths from prior work (check availability):

- Flutter: `/Users/pamithkaluarachchi/development/flutter/bin/flutter`.
- PostgreSQL tools: `/opt/homebrew/opt/postgresql@18/bin/`.
- Previous isolated server: `/tmp/carepulse-audit-pg`, port 55439. Check its status and safety before reuse.
- Backend tests require `CAREPULSE_TEST_CONNECTION`, targeting local PostgreSQL only.
- Local app defaults: backend 5014, React 3000, Flutter web 8080. Avoid interrupting the user's running manual tests; use separate ports/config for assignment automation.

## Human information needed later

Obtain names/student IDs and truthful testing responsibilities, the lecturer's CLEAR/declaration guidance, and confirmed repository/PR references. Do not assign earlier AI-written commits to students or invent their reflections. Students must personally execute/understand/adapt their chosen tests. Ask for live physical-device observations only when required; mark unavailable checks honestly.

## Progress log and precise next action

### 1 October 2026 — start

- User explicitly requested a persistent plan before implementation and then step-by-step completion.
- Read current Git state: clean working tree at start, branch `feature/student4-dispatch`.
- Read existing performance script and database test fixture. Fixture requires an explicit connection and creates/deletes unique databases; isolation is mandatory.
- Created this plan before assignment-specific implementation or test execution.
- No new tests, scans or benchmarks have been run for this assignment yet.

- Completed phase 0 source/tool inventory: see [BASELINE_INVENTORY.md](BASELINE_INVENTORY.md). Baseline commit `1c638e6`; 11 React and 7 Flutter test files identified. Framework commands and local PostgreSQL binaries found; k6/ZAP not found on PATH. No test counts inferred from file counts.

### 1 October 2026 — phases 1 and 2

- Created TEST_PLAN.md and 26 scenario-group rows in TEST_CASES.csv; member assignments are proposed, not authorship claims.
- Started isolated PostgreSQL `/tmp/carepulse-audit-pg` on port 55439. No Neon testing performed. Server is currently running; stop it after work if no longer needed.
- Captured baseline reports in `docs/qm/evidence/baseline-20261001/`: backend 116/116, React 33/33, Flutter 39/39; web production build passed. Full commands and limitations in EVIDENCE_INDEX.md; totals in EXECUTION_SUMMARY.md.
- API suite coverage 90.09% lines / 49.02% branches. Separate safety-suite coverage is not additive. Do not claim frontend/mobile or clinical coverage from these figures.
- No application code changed. Evidence files include local paths; perform final secret/artifact review before committing.

### 1 October 2026 — targeted gap review (phase 3)

- Added 7 consultation cases (future visit, identity mismatch, note/prescription boundaries), 5 deterministic safety threshold cases, and extended the existing concurrent-nurse test through completion and assignment of the waiting case.
- Full backend suite passed: 121 API + 7 safety = 128, zero failures/skips. No production fix was needed. See GAP_REVIEW.md and evidence/gap-tests-20261001; test diff captured with the base commit.
- Initial baseline environment completed in metadata: Flutter 3.47.1 / Dart 3.13.1; PostgreSQL 18.4; .NET 8.0.423. Node 22/npm 10 versions are in web.log.
- Current case matrix has 30 scenario groups. Group counts are not framework case counts.
- PostgreSQL test server remains running on local port 55439. Production/shared Neon untouched. No commits or pushes made.

### 1 October 2026 — cross-client service integration (phase 4)

- Added standalone `tests/CarePulse.ClientHarness` (not in solution): WebApplicationFactory + real loopback HTTP forwarding host on 5516, local disposable PostgreSQL only, seeded synthetic profiles/tokens, deterministic AI.
- Added opt-in React `src/qm/dispatch.client.test.js` using real triage API and Zustand store; ordinary runs skip without CAREPULSE_CLIENT_SESSION.
- Added `mobile-app/qm/cross_client_test.dart` using production Flutter triage/nurse services over real HTTP; secure storage mocked. This is outside ordinary Flutter test discovery.
- Four ordered phase tests passed. Flutter submitted; React approved/assigned; Flutter tracked/arrived/completed and observed patient completion; React confirmed completion/nurse availability. Evidence in client-integration-20261001, commands/scope in CLIENT_INTEGRATION.md.
- First React attempt hit sandbox EPERM; authorized rerun passed. This is an environment issue, not an application bug.
- Full UI E2E row INT-02 stays Not run. Added passing service-level row INT-03. Do not claim browser/emulator UI, GPS hardware, live login, CORS, or live model coverage.
- Harness stopped normally after the run. Local PostgreSQL server remains on 55439. No Neon changes, commits or pushes.

### 3 October 2026 — phase 5 AI evaluation

- Added standalone tests/CarePulse.AiEvaluation runner using exact application planner prompt/client and three predeclared synthetic cases. No patient DB reads or tool execution in live sample.
- AI_EVALUATION.md records protocol/criteria/limitations; 40 deterministic tests passed before and after the prompt correction.
- Initial three live samples passed schema checks but output review discovered unsupported paging/room/lab suggestions (QM-AI-001). Constrained production AgentPlannerService prompt to actual capabilities and corrected example/doctor gate. Same three-case retest passed structure and AI-assisted output review; preserve before/after JSON and patch.
- DEFECT_REPORT.md records real defect, cause, correction, retest and residual model risk. No clinical or universal prompt-injection-safety claim; live sample covers planner only.
- Evidence: docs/qm/evidence/ai-evaluation-20261003. No changes to shared Neon, commits or pushes.
- Prior temporary DB directory was gone and port 55439 occupied. Created fresh isolated PostgreSQL at /tmp/carepulse-qm-pg-20261003 on port 55441. This test server remains running; do not confuse it with old port. Source runner is outside ordinary CI to avoid accidental provider calls.

### 3 October 2026 — phase 6 performance evaluation

- Extended isolated ClientHarness with opt-in CAREPULSE_PERF_DATA=1 synthetic dataset: 501 patients, 51 doctors, 1001 future slots, 1 nurse.
- Added scripts/qm-performance.py: fixed loopback target, private token-file input, preflight response assertions, 12 warm-ups, four 600-request stages (concurrency 1/5/20/1), per-request data and percentiles/error/throughput summaries.
- Declared thresholds before run in PERFORMANCE_EVALUATION.md: at 5 clients p95 <1000 ms and errors <1%. Actual p95 9.921 ms; zero errors across all 2400 measured requests. 20-client p95 30.684 ms. Short local reads only; no production-capacity or sustained-load claim.
- Evidence in performance-20261003/results.json and metadata.json. Harness stopped normally; its disposable database/token file cleaned up. PostgreSQL server on 55441 remains running for further isolated tests.
- No production code changes in this phase. No Neon usage, live AI load, commits or pushes.

### 3 October 2026 — phase 7 security evaluation

- Added 11 HTTP security cases to IntegratedWorkflowTests: anonymous reads (4), tampered JWT, admin doctor-approval denial, cross-patient upload, invalid uploads (3), invalid triage coordinates. Denied writes checked against fresh DB context.
- Run security-20261003 passed all 17 selected tests (11 new + 6 existing workflow/security tests), zero failed/skipped. No production vulnerability was exposed by these probes; no application change needed.
- SECURITY_EVALUATION.md records protocol/results and tool rationale: xUnit/WebApplicationFactory targeted HTTP security probes, not a ZAP scan or deployed pen test. Existing compile warning EF1002 was inspected: migration fixture interpolates only three constant table identifiers; no externally supplied input. Kept warning/evidence; not counted as confirmed vulnerability.
- Evidence: TRX, security.log, machine results summary and source metadata. Expanded matrix SEC-04 now scoped to targeted probes. Full dependency/TLS/header review not claimed.
- No shared Neon changes, commits or pushes. Isolated PostgreSQL 55441 remains running.

### 3 October 2026 — phase 8 supplementary non-functional checks

- Added four axe-core/Jest component checks for login error state, registration, consultation and dispatch forms. Declared axe-core 4.13.0 explicitly as a development dependency; package lock updated.
- Full web run: 37 passed, one opt-in client-harness test skipped. Four accessibility checks are included in 37, not additional tests. Zero definite axe violations; multiple-label checks incomplete on three forms (14/3/3 nodes). Contrast disabled in jsdom. Manual review remains pending.
- Fresh focused backend reliability run: 2 passed (nurse race/release/reassignment and interrupted-state recovery with forbidden-tool rejection). Flutter session run: 3 passed (expired session, telemetry 401, completion conflict).
- Preserved logs, raw axe/Jest/TRX/Flutter outputs and source hashes in evidence/nonfunctional-20261003. See NONFUNCTIONAL_EVALUATION.md for commands and scope. No production changes needed in this phase.
- Shared Neon untouched; no commit/push. Isolated PostgreSQL 55441 was left running. Full UI E2E INT-02 remains Not run; no device/browser compatibility claim.

### 3 October 2026 — phase 9 consolidated report

- Re-read the original assignment PDF. Prepared SOFTWARE_TESTING_REPORT.md in simple English and the matching four-page PDF. It includes scope, plan, tools, results, integration, required performance/security, defect/retest, limits and conclusion.
- Created SUBMISSION_CHECKLIST.md and scripts/qm-build-report.py (ReportLab 5.0.1) for reproducible PDF generation. Temporary PDF tools were installed in /tmp/carepulse-qm-report-tools, not application dependencies.
- Cross-checked recorded TRX counts and retained separate run totals. Updated stale execution-summary outstanding items. No new application tests were required for documentation changes.
- Report is ready for team review, not declared submission-complete. Names/IDs, personal contributions, final repository/commit links and exact CLEAR instructions remain pending. User supplied IT24101521 — Bandara EMGO; IT24100966 — Kaluarachchige P.E.K; IT24100009 — Walpala M.W.H.T. Fourth member and Student 1–4 mapping are pending. User explicitly confirmed no individual testing tasks completed yet. Added this truthful status to the report; automated coverage is not personal viva completion.
- PDF text/page extraction validated. No push, merge, CourseWeb submission or shared Neon changes.

### 3 October 2026 — phase 10 demonstration handover

- Created INDIVIDUAL_TESTING_GUIDE.md with isolated local database setup, four role-specific commands, expected behavior, explanation/practice tasks, evidence instructions and failure troubleshooting.
- Created CONTRIBUTION_RECORD.md with three confirmed identities, fourth identity/role mapping pending, and truthful personal-execution fields. User confirmed that no individual tasks have been completed; no personal completion or authorship was invented.
- Checked command filters against existing test method names. Clarified that the appointment conflict test prepares stale contexts but calls bookings sequentially. Docker setup is a recipe, not recorded execution evidence.
- Linked the guide and contribution record from the checklist/report and regenerated the PDF. No application code, test reruns, shared Neon changes, commits or submission in this documentation step.

### 4 October 2026 — fourth member confirmed

- Added IT24101037 — Savindu to the report and contribution record; regenerated the PDF. All four identities are supplied. Role mapping and personal execution remain pending.

### 6 October 2026 — Student 4 personal execution

- Kaluarachchige P.E.K (IT24100966), Student 4, personally ran Flutter session tests (3 passed), backend workflow/concurrency tests (2 passed), and safety tests (7 passed initially).
- Student created isolated Docker PostgreSQL `carepulse-qm-viva` on localhost 55442 and supplied readiness output. No shared Neon tests requested.
- Student added the Codex-suggested boundary case `[InlineData(8.49, 31, false, true)]` and reran safety tests: 8 passed. This is an AI-assisted student-applied extension, not independent suite authorship.
- Verified personal backend/safety TRX counters and the saved Flutter expanded log. Links are in CONTRIBUTION_RECORD.md. Latest selected suites cover 13 distinct cases, not a fresh full-suite run.
- Student requested explanation of the tests; explanation was provided, but independent understanding/viva readiness is not yet verified. CLEAR instructions/declaration remain pending. Other members' personal execution remains unconfirmed.
- The QM PDF has not yet been regenerated with these personal results. Next: obtain lecturer's declaration instructions, reconcile report with contribution record and regenerate PDF; preserve earlier group results as historical evidence.
- Student subsequently applied the Codex-provided expired-session service test and personally reran Flutter: 4 passed, saved in `evidence/personal/student4-flutter-run1/extended-results.log`. Verified source and log. Latest personal selected-suite count is 14 (4 Flutter + 2 backend + 8 safety), superseding 13 above. Independent understanding remains unverified; record AI assistance honestly.

**Next action:** team members follow INDIVIDUAL_TESTING_GUIDE.md and fill CONTRIBUTION_RECORD.md with actual runs and any personally implemented changes. Map names to Students 1–4 and obtain exact CLEAR instructions. Review outstanding manual UI/accessibility/device checks. Then incorporate confirmed details/results, regenerate the PDF, review secrets and final Git/CI evidence, and submit through CourseWeb. Prepared artifacts do not complete personal viva requirements; do not mark the whole assignment complete yet.
