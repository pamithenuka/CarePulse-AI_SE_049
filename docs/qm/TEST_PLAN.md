# CarePulse quality evaluation — test plan

Version 1, 1 October 2026. Requirement source: Software Testing and Quality Evaluation assignment, due 5 October 2026. Working baseline: `1c638e6`; record exact commit and uncommitted changes for each execution. See [progress tracker](ASSIGNMENT_PROGRESS.md).

## Objectives and scope

Verify that the implemented patient records, symptom triage, appointment scheduling and nurse dispatch functions behave correctly together. Evaluate authorization, data integrity, safe AI failure, reliability and response time using executable tools. Preserve reproducible evidence and acknowledge product limitations rather than claiming production or clinical certification.

In scope: .NET API, PostgreSQL, React staff application, Flutter patient/nurse application, four-step agent workflow and provider integration. Out of scope: real clinical effectiveness, delivered SMS, real road navigation, multi-hospital access isolation and background GPS (not implemented). Live-model tests measure the selected cases, not universal model safety.

## Risk priorities

| Priority | Risk | Verification |
| --- | --- | --- |
| P0 | Another patient/nurse reads or modifies a private case | Role and ownership API tests, JWT rejection, scoped security probes |
| P0 | Dispatch bypasses approval or unsafe AI output triggers an action | Approval gates, allowed tool/agent validation, adversarial output and fallback tests |
| P0 | Duplicate booking, nurse assignment or vitals corrupts state | PostgreSQL concurrent-write and idempotency tests |
| P1 | Saved records differ between clients or are lost | CRUD, persistence, workflow tests and cross-client demonstration |
| P1 | Provider/network failure is mistaken for success | Bounded retries, error UI, expired-session and recovery tests |
| P1 | Expected load causes errors or excessive latency | Seeded local workload with predeclared thresholds |
| P2 | Forms/navigation prevent users completing tasks | React/Flutter validation tests plus scoped accessibility/usability checks |

## Tools and test layers

| Area | Tools | Reason and boundary |
| --- | --- | --- |
| Backend/API | xUnit, Moq, WebApplicationFactory | Assert services and HTTP behavior; fake external services explicitly |
| Database | xUnit with isolated PostgreSQL | Verify real constraints, transactions and migrations; EF in-memory tests alone are insufficient |
| React | Jest, React Testing Library | Verify visible state, forms and protected routes; mocked network is not live API evidence |
| Flutter | flutter_test | Verify validators, widgets and session/API behavior; supplement with device workflow evidence |
| Integration | Existing HTTP workflow suite; cross-client automation where needed | Exercise role transitions and persisted state, then identify client coverage gaps |
| AI | xUnit deterministic cases and small synthetic live evaluation | Separate orchestration safety from stochastic model behavior |
| Performance | Bounded scripted workload, k6 if feasible | Reproducible latency/error/throughput measurements; document tool choice and machine limits |
| Security | Executable authorization/input probes; ZAP if feasible | Verify concrete access controls and inspect HTTP findings; scan warnings require manual triage |
| Reliability | Recovery/concurrency/session tests | Relevant to interrupted dispatch and repeat submissions |
| Accessibility/compatibility | Targeted automated UI check and device/browser record | Narrow supplementary scope; do not claim WCAG certification |

## Environment and data

Use local .NET 8, Node 22/npm, Flutter 3.47.1 and isolated PostgreSQL. Record actual versions, OS, ports, dataset size and configuration mode in each run. Use separate ports from the user's manual-testing apps. Tests must never use the Neon connection from `appsettings.Local.json`.

Use synthetic named patients, doctors, nurses, clinical inputs and disposable uploads. Keep secrets out of evidence. Test fixtures create/drop unique databases and therefore require a local server with that permission. Live Gemini calls must be limited synthetic evaluation, excluded from load tests, and labeled with model and date.

## Test design and execution

[TEST_CASES.csv](TEST_CASES.csv) is the initial requirement-to-test matrix. Each ID represents a scenario or explicitly named scenario group; framework test counts and matrix row counts are different and must be reported separately. Expand parameterized groups when recording their actual outcomes. Existing method references are execution targets, not proof of a pass.

1. Save environment metadata, baseline commit and change inventory.
2. Run existing suites and preserve raw output even when failures occur.
3. Map individual results to cases; a suite's exit code alone cannot prove an unimplemented case.
4. Add tests for important uncovered paths; fix defects and rerun affected tests, then integration regressions as appropriate.
5. Execute integrated, AI, performance and security scenarios with clear boundaries.
6. Interpret results, limitations and remaining risks; assemble reports and demonstrations.

Initial performance target (project test target, not clinical SLA): seeded authenticated non-AI requests should have <1% unexpected errors and p95 <1 second at 5 concurrent clients on the recorded local machine. Baseline and 5-client runs precede a bounded 20-client exploratory stress run; stress results characterize limits rather than retroactively change the target. Final workload mix, duration and dataset must be recorded before starting performance execution. Never load-test Gemini or shared Neon.

Security acceptance: unauthenticated/expired requests rejected; cross-account access denied without data leakage; forbidden roles cannot approve/complete another user's actions; invalid uploads and payloads fail clearly. Document unresolved findings and do not label unverified scanner alerts as exploitable defects.

## Responsibilities and schedule

Proposed responsibilities are by project domain, **not claims of completed personal authorship**. Names/IDs and agreement remain pending.

| Owner | Proposed meaningful demonstration | Shared responsibility |
| --- | --- | --- |
| Student 1 | Patient ownership/records and invalid profile or upload test | Security matrix and identity evidence |
| Student 2 | Planner/triage output validation, adversarial or failed provider case | AI evaluation and explanations |
| Student 3 | PostgreSQL booking conflict/concurrency and roster/consultation tests | Performance workload and database evidence |
| Student 4 | Approval/assignment/telemetry/vitals and recovery test | Integrated chain and reliability evidence |

Each member must personally run, understand and adapt their tests and retain truthful Git/evidence references. Schedule: 1 Oct plan/baseline; 2 Oct gaps/integration/AI; 3 Oct performance/security/reliability; 4 Oct retesting/report; 5 Oct demonstration/submission checks. Adjust transparently if work slips.

## Entry, exit and suspension criteria

Entry: dependencies available, isolated database verified, synthetic accounts/fixtures ready, evidence paths established. Stop an execution if its target could be shared Neon, it exposes secrets, or the environment is not the recorded one.

Exit: all required areas executed with tool evidence; P0/P1 failures fixed and retested or explicitly reported as unresolved; full integrated chain evidenced; case/defect/summary totals reconciled; final report PDF and reproducible scripts ready. Do not convert Not run/Blocked into Pass. Human viva and submission remain pending until students complete them.

## Evidence and defect handling

Use `docs/qm/evidence/<run-id>/` for sanitized small artifacts and metadata. Exclude build caches, secrets and sensitive payloads. Preserve raw reports; sanitized copies must be identified. Record command, timestamp, versions, commit, working-tree changes, AI mode, scope and exit status. Retain failing and passing retest outputs when available.

Defect fields: ID, affected case/version, severity/priority, preconditions, reproduction, expected/actual, evidence, cause, fix commit, retest command/result and residual risk. Historical fixes without original failure output must say so. Do not manufacture contribution or defect evidence.
