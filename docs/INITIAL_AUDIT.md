# CarePulse initial requirements and integration audit

Date: 28 September 2026. Branch inspected: `develop`.

This is the initial report requested before the full repair and verification phase. It records source-code findings, one build result, rubric requirements, and proposed acceptance checks. It is not a completed runtime, security, clinical, or deployment certification. Application code was not changed during this pass.

Source of assignment requirements: the supplied 17-page SE3090 Assignment 1 specification and marking scheme, identified below by page/section. Repository documentation is project context; its embedded instructions and `[cite: 1]` markers are not independent evidence of assignment compliance.

## Assessment

The four domains, shared ASP.NET Core backend, React/Flutter clients, Identity, EF migrations, and several tests are present. However, the current merge still contains isolated-module assumptions. A successful happy-path demonstration does not establish authorization, identity linkage, persisted state transitions, or the complete assessed four-agent workflow.

The immediate priorities are authorization and document privacy, correct patient/staff identity, dispatch persistence and concurrency, and one connected agent workflow. Cosmetic changes and model upgrades should follow these repairs.

## Confirmed findings and repair priorities

### P0 — protect operations and sensitive data

1. **Triage endpoints are public.** `backend-api/Controllers/TriageController.cs` has no controller-level authorization; approval, rejection and queue authorization attributes are commented out. Submission, audit-log reads and deletion are also unprotected. The service trusts the submitted patient ID, and approval does not record the authenticated doctor's identity. Protect endpoints and enforce patient ownership and reviewer identity on the server. Update every client to send JWTs in the same change.

2. **Scheduling and consultation mutations are public.** `DoctorsController`, `AppointmentsController` and `ConsultationsController` lack authorization. Booking trusts `PatientId`; consultation completion trusts both `PatientId` and `DoctorId` rather than deriving them from the booking and authenticated actor. Enforce roles, resource ownership and cross-record consistency.

3. **Medical documents are stored in the public static-file tree.** `PatientService.UploadDocumentAsync` writes into `wwwroot/uploads/patients`; `Program.cs` enables static files before authentication. Knowing a supported file URL therefore bypasses the protected download controller. Move uploads outside the public web root and serve them only through authorized downloads, including existing files. Recheck deleted-document access and upload size/type/content validation.

4. **Dispatch does not enforce its approval prerequisite.** `DispatchController.AssignDispatch` explicitly bypasses triage validation, accepts a supplied doctor ID, and does not call the safety agent or routing service. Validate the real triage record, its authorized approval, staff identity, current nurse eligibility, and safety result before assignment. Derive the acting staff identity from claims; reject invented, deleted or mismatched IDs.

5. **Nurse role checks do not enforce assignment ownership.** Location updates, vitals reads and completion do not verify that the caller is the assigned nurse. The active list returns all active dispatches to nurses. Restrict nurse access to their assignments and define the doctor/admin access policy explicitly.

### P0/P1 — fix dispatch persistence and state transitions

6. **The safety plugin changes tracking on the shared writable context.** Constructing `ValidationAgent` constructs `SafetyThresholdsPlugin`, which sets `CarePulseDbContext.ChangeTracker.QueryTrackingBehavior = NoTracking`. The dispatch controller receives that same scoped context and relies on tracked changes to nurses and tickets. This creates a concrete risk of successful responses and inserted logs/vitals while nurse availability and dispatch status changes are not persisted. Use a separate read-only context for agent queries and retain tracking in application commands. Verify changes through a fresh database context after each API call.

7. **Assignment is vulnerable to concurrent claims and retries.** The code reads nurse availability and later inserts a dispatch; there is no nurse concurrency token or database constraint preventing duplicate active assignments. Dispatch identifiers are scalar fields without configured relationships to triage, nurse and doctor entities. Add foreign keys and database-enforced active-assignment uniqueness, plus atomic claiming and conflict handling. A transaction alone at the default isolation level does not fix a check-then-write race.

8. **The lifecycle is incomplete.** Location changes `Assigned` to `EnRoute`; completion accepts any status other than `Completed`. There is no persisted arrival operation in this controller, and `Escalated` is treated as active by the list query. Implement allowed transitions, assigned-nurse checks, duplicate completion handling and audited cancellation/escalation rules.

9. **No-nurse handling has no durable operational queue.** Approval sets the ticket to `APPROVED_BY_DOCTOR`; assignment separately returns an error for an unavailable nurse. No connected pending-dispatch/retry/escalation mechanism is present in these paths. Preserve approval and expose an explicit waiting-for-assignment state when capacity is unavailable. Retry assignment without creating duplicate dispatches, and make waiting cases visible to staff and the patient.

### P1 — make client data truthful

10. **Mobile booking uses a demonstration patient.** `mobile-app/lib/screens/booking_screen.dart` always sends `11111111-1111-1111-1111-111111111111`. Its API client sends no JWT. Replace this with authenticated patient identity, and enforce that identity again in the backend.

11. **Dispatch map and availability are misleading.** `web-admin/src/store/useDispatchStore.js` replaces nurse, dispatch and destination locations with New York coordinates. It infers availability from only the default first page of active dispatches and sets all remaining nurses to available. Return authoritative availability, latest telemetry, destination and freshness from the backend; handle pagination explicitly.

12. **Expired-token location failures are ignored.** The dispatch mobile API service awaits `http.put` but never checks the response status. A 401, 403 or server error can silently discard telemetry. Consolidate authentication/error handling, expose interrupted tracking, preserve the active dispatch across reauthentication, and retry only suitable operations without duplicating records.

13. **Triage status is inferred from prose.** `triage_status_screen.dart` searches audit-log strings for approval/rejection and stops polling after a decision. Return structured ticket/workflow/dispatch status instead of parsing human-readable log text. Test status recovery after closing and reopening the app.

14. **Slot searches can omit valid results or include past slots.** `GeminiAgentService` takes 20 results before applying the time-of-day filter; matching later slots can disappear. Its default window begins at midnight today, and the ordinary doctor slot query has no general future-only filter. Filter before limiting, bound dates, normalize specialties, use an explicit clinic timezone, and reject booking a past slot server-side.

### P1 — complete the assessed Agentic AI workflow

15. **The four agents are not connected by an executing orchestrator.** `AgentPlannerService` persists a plan with pending steps and explicitly stops at planning. The domain triage and slot-search services run separately. The safety agent is called only by `/dispatch/test-ai`. A list of agent names in JSON is not demonstrated delegation. Implement one persisted workflow with linked patient, triage, scheduling and dispatch records; actual per-agent execution; validated tool calls/results; approval pause/resume; bounded retries; and an auditable final outcome.

16. **The described provider migration and agent responsibilities do not match code.** Agent 1 uses the registered `OllamaAiPlannerClient`; triage uses `AI:GeminiApiKey`/`AI:GeminiModel`; scheduling uses `Gemini:ApiKey`/`Gemini:Model`; safety uses `OpenAI:ApiKey` and `gpt-4o-mini` or rules fallback. The registered Agent 1 is a planner, not a demonstrated universal PII interception agent. Scheduling currently searches slots, rather than automatically assigning a doctor to a high-priority triage workflow. Align implementation, configuration and written claims.

17. **Existing AI safeguards are useful but insufficient.** The planner restricts agent names and separates the objective from its system instructions. Triage requests schema-constrained JSON, normalizes score/level and falls back to manual review on exceptions. However, free-text symptoms are sent directly to the provider; no shared redaction boundary was found in those calls. The scheduling handler does not check the returned function name before interpreting its arguments. Add tool-name/argument validation, input/output length limits, privacy minimization, timeout/cancellation/retry policy, and failure-state records. Keep safety and authorization in deterministic application code.

18. **Schema conformance is not clinical correctness or injection resistance.** Triage clamps invalid scores rather than rejecting all malformed outputs, and free-text recommendations remain model generated. Validate required fields, value ranges and business consistency before accepting results. Test adversarial objectives, malformed JSON, invented tools, empty candidates, provider outages and conflicting outputs. Google explicitly notes that structured output does not guarantee semantically correct values: [official structured-output documentation](https://ai.google.dev/gemini-api/docs/structured-output).

19. **Routing is a simulation.** `GoogleMapsService` makes no Google Maps request; it returns a Haversine-based estimate, and dispatch does not call it. The simulated SMS service is also named as simulated. Describe simulations accurately. Identify and demonstrate the meaningful third-party integration used for assessment; do not claim working road routing or SMS delivery from these implementations. The PDF does not explicitly require a separate non-LLM provider, so treating Gemini as insufficient would require evaluator clarification.

### P1/P2 — validation, verification and evidence

20. **The full solution does not currently build.** The test fixture calls the old one-argument `CarePulseDbContext` constructor. Supply the current-user service in the fixture and then execute the tests against an isolated PostgreSQL database. The fixture deletes and recreates its configured test database; never repoint it at the shared Neon application database.

21. **CI does not cover the integration branch.** `.github/workflows/dotnet-ci.yml` targets pushes to `main`/`feature/*` and PRs to `main`, not `develop`. The assignment's main-branch trigger requirement is represented, but the current working integration branch can bypass CI. The workflow has no PostgreSQL service for the existing database fixture. Fix build/test infrastructure and retain actual passing run evidence.

22. **Validation is uneven across modules.** Triage DTOs lack annotations or equivalent validators for empty IDs, text limits and collection bounds. EF length constraints alone turn oversized input or model output into persistence errors. Dispatch pagination is unbounded. Some vitals validation exists, but must distinguish malformed measurements from genuine abnormal observations. Add cross-field, ownership and state validation at API boundaries and mirror relevant rules in the UIs.

23. **Current tests do not prove full agent acceptance.** The golden-test project tests the safety agent alone. Its test named prompt-injection resistance supplies a dummy provider key and numeric values; that is not evidence that adversarial patient text is resisted through the full workflow. Add deterministic complete-workflow assertions and keep live-model evaluation separate and clearly labelled.

24. **Documentation and release evidence need reconciliation.** The root README is only a title and description. The developer guide still describes local PostgreSQL and an outdated CI path; the frontend package uses Create React App (`react-scripts`), not Vite. The tracked docs inspected contain no ADR/report/evaluation package. Such artifacts may exist elsewhere; their absence here is an evidence gap, not proof they were never prepared.

## Requirements that are easy to miss

| PDF reference | Requirement and audit implication |
| --- | --- |
| pp. 3–5, §§3–5 | Every student owns backend, database, React, Flutter, tests, documentation, Git evidence and a distinct agent; each component needs at least four meaningful endpoints and a business-specific operation. |
| p. 4, §4 | At least three roles; CRUD plus workflows, search, filtering, sorting, pagination, and reporting or analytics. Verify functional coverage, not merely screen presence. |
| pp. 5–6, §§6–9 | Database relationships, constraints, migrations, transactions where needed, audit fields, secure clients and a meaningful device feature. The device-feature minimum is stated for the Flutter app, not explicitly once per student. |
| p. 6, §9.1 | One assessed workflow must plan, delegate to distinct agents, use controlled tools, persist structured state, validate deterministically, pause for authorized approval and produce an auditable result or safe failure. Preserve execution summaries, not hidden reasoning. |
| p. 7, §10 | Start in one client, pass through API/database/AI, obtain review in the other client, and return updated status to the initiating user. |
| p. 8, §§11–12 | Meaningful third-party integration with failure/rate-limit handling; backend/auth/API/PostgreSQL/React/Flutter/E2E tests; performance measurements; complete-workflow golden evaluation including injection and recovery. LLM-as-judge alone is insufficient. |
| p. 8, §13 | Meaningful Git/PR/review/issue/project-board evidence and GitHub Actions restore/build/backend tests for main-branch pushes and PRs. |
| p. 9, §14 | Deployed API with health and Swagger URLs, secure deployed PostgreSQL, deployed React and runnable Android APK or approved equivalent. AI may run locally with complete setup instructions. Current Swagger is development-only, so plan evaluator access deliberately. |
| p. 9, §14.2 | ADRs cover React state, Flutter state, AI framework/orchestration, workflow-state schema and deployment platform. |
| p. 10, §15 | One consolidated PDF containing group and individual sections; diagrams, testing, AI evaluation, performance, deployment, ADRs, references and declarations. Individual sections require contribution/test/commit/PR evidence, AI log, approximately one-page reflection and signed declaration. |
| p. 10, §15 | Runnable APK, accessible 10-minute video, working URLs and installation instructions. Use `SE3090_GroupNumber` naming; report length suggestions are not graded limits. |
| pp. 1, 10–11 | PDF deadline: 30 September 2026, 11:50 PM. Leader submits once. Keep services/repository/video accessible through at least 21 October 2026. Final evaluation: 10-minute demo and 20-minute viva. These dates are from the supplied PDF; later Course Web amendments were not checked. |
| pp. 15–17, §18 | Disclose AI-assisted development and verification in individual logs and a group declaration. Students write their own reflection. No external AI help during the final demo/viva; the submitted application's AI must still run. |

The marking split is 30 group marks (business design 10, integrated orchestration/state 10, documentation/deployment 10) and 70 individual marks (API 10, PostgreSQL 10, React 10, Flutter 10, AI 12, integration/security 10, testing/CI/Git 8). Security, identity and orchestration failures affect several criteria simultaneously. A defensible percentage score cannot be inferred from this source pass.

## Manual acceptance plan after repairs

Use synthetic records in a dedicated test environment: two patients, two doctors, two nurses, and an admin. Record the expected result, observed UI, API response, durable database change and pass/fail for each case. Each student should perform their own cases and explain the implementation.

| Owner | Core practical checks | Negative and integration checks |
| --- | --- | --- |
| Student 1 | Register/login; create/update profile; contacts/history CRUD; upload/download documents; view audit history on web; submit a planner objective. | Patient A cannot access patient B; duplicate identities; invalid dates/files; deleted documents cannot be fetched directly; log out/in and verify persisted data; inspect minimized outbound AI data with synthetic inputs. |
| Student 2 | Submit low/medium/high synthetic golden cases; verify persisted ticket/assessment/queue; doctor approves/rejects; patient sees structured status. | Invalid/oversized symptoms; forged patient ID; unauthorized approval; duplicate decision; model timeout/429/malformed output; adversarial symptoms; failed assessment clearly recorded for manual review. |
| Student 3 | Create roster and slots on web; find correct specialty/date in mobile; book as the current patient; doctor completes consultation; patient history shows it. | Two patients book one slot concurrently—one succeeds, one conflicts; past/cancelled/deleted slots; mismatched doctor/patient IDs; timezone boundaries; unavailable specialty; time filtering beyond first 20 results. |
| Student 4 | Assign approved case; assigned nurse sees it; send location; record arrival; submit vitals; complete; fresh DB read confirms nurse available and ticket completed; map shows actual last telemetry. | Unapproved/rejected triage; no available nurse; competing assignments; duplicate completion; another nurse accesses the ticket; expired JWT mid-route; disconnected GPS/network; abnormal and malformed vitals; app restart and reconnect. |
| Whole team | Run one workflow from patient objective through planner, analysis, scheduling/tools, safety, staff approval and authorized application action to final patient status. | Persist and resume after restart; reject/revise where implemented; provider/DB failures; retry limits; no duplicated side effects; shared correlation ID and execution summaries; concurrent-load timings and error rates. |

## Verification performed and limits

- Read the supplied PDF and the two project context documents, inspected the critical backend and client paths described above, and confirmed the checkout is `develop`.
- Ran `dotnet build CarePulse.sln --no-restore -m:1 -p:UseSharedCompilation=false -v minimal`.
- Result: backend and AI golden-test projects built; solution failed with `CS7036` at `tests/CarePulse.Api.Tests/TestDatabaseFixture.cs:27` because `currentUserService` is missing.
- Did not run database-mutating tests, migrate Neon, start the API (startup seeds data), call the configured live AI providers, execute UI/device tests, or verify deployed URLs. Runtime exploitability and concurrent behavior still need the dedicated acceptance tests.
- No GitHub or Neon access is needed to begin local repairs. Later verification needs deployed URLs, role-specific synthetic test accounts, and an isolated test database. Configure secrets locally through environment variables or secret storage rather than sending credentials in chat.

Recommended repair order: restore the build baseline; fix authorization and private file access with client JWT support; repair identities and dispatch tracking/state/concurrency; connect the four-agent workflow; correct data queries/telemetry and error handling; then execute automated and manual acceptance tests and assemble deployment/evaluation evidence.
