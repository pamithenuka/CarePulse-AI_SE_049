# Assignment baseline inventory

Inspected 1 October 2026, commit `1c638e6`, branch `feature/student4-dispatch`. This is source/configuration inspection, not a new test run.

| Requirement area | Existing source/evidence | Evidence gap for assignment |
| --- | --- | --- |
| Backend/API | `tests/CarePulse.Api.Tests/`: services, controllers, booking, triage, Gemini transport | Fresh machine-generated results and case-to-test mapping |
| PostgreSQL | Migration, scheduling, booking and integrated tests; explicit local test connection | Fresh isolated database runs; report integrity/transaction assertions |
| React | 11 test files across forms, protected routes, patient pages and dispatch state | Current results, coverage interpretation and missing-path review |
| Flutter | 7 test files for services, sessions, validators, widgets and role selection | Current results, navigation/form coverage review; no full device automation established |
| Integrated workflow | `IntegratedWorkflowTests.cs`; app manual plan | Identify actual asserted workflow and mocked boundaries; supplement cross-client demonstration |
| Agentic AI | Planner service/client tests, transport tests, safety golden tests, workflow tests | Map malicious inputs/tools/rules/recovery; separate live results from deterministic tests |
| Performance (required) | `scripts/performance-smoke.py`, historical JSON sample | Meaningful seeded workload, declared thresholds, multiple load levels and report |
| Security (required) | Existing role, ownership, token and workflow enforcement tests | Explicit security case matrix, fresh outputs, suitable scan and finding analysis |
| Additional non-functional | Recovery/concurrency/session tests | Justify reliability selection; scope accessibility/compatibility checks |
| Reports and defects | Historical audit, manual plans, CI workflow | Completed cases, formal defect/retest records, consolidated PDF and evidence index |
| Individual demonstration | Test source and repository history | Actual member attribution, personal execution and viva practice; CLEAR guidance |

## Installed-tool discovery

Found on PATH: dotnet, node, npm, Flutter, Docker. Local PostgreSQL 18 binaries exist. This does not yet establish versions, Docker daemon readiness or database server state. Neither `k6` nor `zap.sh` was found on PATH; an app/container installation may still exist. Do not install redundant tools before checking feasibility.

## Baseline interpretation

No requirement is marked fully complete solely from this inventory. Existing automation substantially reduces the work needed, but results must be rerun and tied to the actual submission version. Historical manual screenshots and fixes may support reports only with accurate dates and limitations.
