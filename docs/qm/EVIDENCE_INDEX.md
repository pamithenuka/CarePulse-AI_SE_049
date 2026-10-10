# Evidence index

## baseline-20261001 — baseline suites

Artifacts: [run directory](evidence/baseline-20261001/). See `metadata.json` for commit, timestamp, environment and working-tree state. These are actual local tool outputs, not screenshots of an assumed pass. Reports may contain local filesystem paths and synthetic fixtures; no production patient data was used.

Environment: macOS ARM64, .NET 8.0.423; web execution explicitly used Node 22/npm 10 via npx (exact output in web.log). Backend database: isolated PostgreSQL on loopback port 55439. No shared Neon access, real SMS or live Gemini in this run. Flutter test execution used the project's existing SDK; add exact SDK version metadata before final submission.

| Evidence | Outcome | Scope |
| --- | --- | --- |
| `backend/*.trx`, `backend.log` | 114 API/service/integration + 2 safety tests passed, 0 failed/skipped | Backend assertions, including real PostgreSQL and mocked providers |
| `backend/*/coverage.cobertura.xml` | API suite backend line coverage 90.09%, branch 49.02%; separate safety suite line 1.32%, branch 1.77% | Separate collector runs, not summed; coverage is not proof of correctness |
| `web-results.json`, `web.log` | 33 tests passed; production build succeeded | 11 Jest suites, mocked client APIs; not live browser E2E |
| `flutter.jsonl`, `flutter-stderr.log` | 39 visible tests passed | Unit/widget/service tests; excludes hidden test-runner setup entries, not emulator E2E |

### Reproduce

Install dependencies using the main README. Start an **isolated** PostgreSQL server. Set `CAREPULSE_TEST_CONNECTION` privately to that local server (fixture creates and deletes unique databases). Do not use Neon. From root:

```sh
dotnet test CarePulse.sln --configuration Release --logger trx --results-directory YOUR_NEW_RUN/backend --collect:"XPlat Code Coverage"
```

From `web-admin`, with Node 22/npm 10:

```sh
npm test -- --watchAll=false --runInBand --json --outputFile=YOUR_NEW_RUN/web-results.json
npm run build
```

Set environment `CI=true` for both commands. From `mobile-app`:

```sh
flutter test --machine
```

Capture stdout/stderr and actual exit codes into a new directory; do not overwrite this baseline. Replace `YOUR_NEW_RUN` with an absolute output directory (or a path relative to the command's working directory).

The baseline backend test command and web build/test command exited 0; Flutter test command exited 0. Coverage was collected without changing application code. Existing non-failing warnings remain in logs and should be interpreted separately from failures.

## gap-tests-20261001 — targeted gap regression

Artifacts: [run directory](evidence/gap-tests-20261001/). `metadata.json` records command, base commit and environment; `changes.patch` identifies the uncommitted test changes. TRX reports and `backend.log` record **121 API + 7 safety tests passed**, exit code 0. No coverage collection was requested for this run. Baseline coverage must not be presented as recomputed.

Reproduce with the same isolated `CAREPULSE_TEST_CONNECTION` setup and the command in metadata, choosing a new output directory. See [GAP_REVIEW.md](GAP_REVIEW.md) for assertion scope. No live Gemini, real patient records or shared Neon was used.

## client-integration-20261001 — real client service handoff

See [CLIENT_INTEGRATION.md](CLIENT_INTEGRATION.md) for exact commands and substitutions. Four ordered phase tests passed (2 Flutter, 2 Jest) over real loopback HTTP and isolated PostgreSQL. `metadata.json` records source hashes because harness/client test files were uncommitted. The initial sandbox EPERM output is retained separately; the permitted retry succeeded. No live provider, UI clicks or physical-device acceptance is claimed. Temporary role tokens are excluded from evidence. The harness was stopped normally after execution.

## ai-evaluation-20261003 — deterministic checks and live planner sample

Protocol/results: [AI_EVALUATION.md](AI_EVALUATION.md). Defect: [QM-AI-001](DEFECT_REPORT.md). Initial and corrected live outputs are preserved separately; there were three generations per run. Forty deterministic cases passed before and after the fix (not 80 unique cases). Metadata includes the exact filter, local database port, model and source hashes. `prompt-fix.patch` records the production prompt correction. No private settings or key is included. Tests used .NET/xUnit with isolated PostgreSQL; live evaluation used GeminiAiPlannerClient without any database access.

Reproduce deterministic tests with the filter in metadata, `dotnet test CarePulse.sln --configuration Release --filter '<filter>' --logger trx --results-directory <new-directory>` and an isolated local CAREPULSE_TEST_CONNECTION. Reproduce live cases using the AI evaluation guide. Later output review is documented separately from immutable raw JSON.

## performance-20261003 — seeded local HTTP workload

Protocol, exact rerun instructions and results: [PERFORMANCE_EVALUATION.md](PERFORMANCE_EVALUATION.md). `results.json` includes all 2,400 measured samples, stage summaries, dataset and acceptance decision; `metadata.json` records source hashes/base commit/runtime. Five-client acceptance passed (p95 9.921 ms, 0% errors). No production or sustained-load capacity claim. A Python standard-library runner supplies reproducible tool evidence; no live AI traffic. Private tokens and full backend request logs are excluded.

## security-20261003 — targeted HTTP security checks

Protocol and scope: [SECURITY_EVALUATION.md](SECURITY_EVALUATION.md). Seventeen tests passed: eleven new probes plus six existing integration/security tests. `results-summary.json` extracts individual TRX outcomes; metadata records source hash, commit, command and loopback database scope. No ZAP/dependency scan or deployed TLS claim. The EF1002 compile warning was reviewed and explained in the report.

## nonfunctional-20261003 — reliability and component accessibility

Four axe-core component tests passed with zero definite violations; multiple-label checks on three forms remain incomplete and contrast was disabled. Full React regression passed 37 tests with one opt-in harness test skipped; the four axe tests are included in 37. Fresh backend reliability checks passed 2 tests and Flutter session checks passed 3. These overlap earlier suite coverage and must not be added as unique new cases. Raw JSON/TRX/logs and source hashes are preserved in evidence/nonfunctional-20261003. See [NONFUNCTIONAL_EVALUATION.md](NONFUNCTIONAL_EVALUATION.md) for reproduction, limitations and pending manual checks. No full UI/device or WCAG conformance claim.
