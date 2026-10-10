# Cross-client service integration evidence

## What was executed

Run `client-integration-20261001` used the production Flutter API/service code and React API/Zustand store code communicating over real loopback HTTP to the ASP.NET application and a disposable local PostgreSQL database.

1. Flutter `TriageApiService` submitted a synthetic case and checked patient identity and approval requirement.
2. React `triageApi` found and approved that same case; `useDispatchStore` saw it waiting, then active after assignment.
3. Flutter nurse `ApiService` read the assignment, sent coordinates, confirmed arrival and submitted vitals. Flutter switched to the patient token and saw `VISIT_COMPLETED`.
4. React fetched again and verified no active dispatch, nurse available, and the same completed patient case.

All four phases passed. The initial React attempt failed with sandbox `EPERM` before reaching the API; allowing loopback access resolved it without changing application code. Preserve this as an environment execution issue, not a production defect.

## Scope and substitutions

This is **cross-client service integration**, not a full browser/emulator UI E2E test. No actual screen buttons, GPS hardware, registration/login screens, camera or physical devices are exercised. Synthetic role tokens and profiles are seeded by the harness. Flutter secure storage and browser localStorage are substituted, but real client serialization, session access, response parsing and React store transformations run. Axios uses its Node HTTP adapter instead of browser XHR, so browser CORS is not tested.

The API runs in ASP.NET WebApplicationFactory, exposed through a loopback Kestrel forwarding host on port 5516. Authorization middleware, application services and PostgreSQL execute normally. Planner and diagnostic AI are deterministic fixtures; safety uses fallback rules. This is not evidence of live Gemini quality. Existing app-only manual testing remains required for UI/device acceptance. A full UI automation case remains explicitly Not run.

## Reproduce

Prerequisites: restored .NET, web and Flutter dependencies; an isolated local PostgreSQL server whose user can create/drop databases. Never use Neon. Do not run parallel harness instances on port 5516. The harness only accepts PostgreSQL host `127.0.0.1` or `localhost`.

On macOS/Linux, from the repository root, Terminal 1:

```sh
export CAREPULSE_TEST_CONNECTION='Host=127.0.0.1;Port=55439;Database=postgres;Username=YOUR_LOCAL_POSTGRES_USER'
export CAREPULSE_CLIENT_SESSION=/tmp/carepulse-qm-client-session.json
dotnet run --project tests/CarePulse.ClientHarness
```

Replace the database username/port with your isolated server settings. Wait for `Now listening on: http://127.0.0.1:5516`. The host creates a unique database and a temporary token handoff file. Keep that file private and outside the repository. It is deleted on normal shutdown. Do not submit it as evidence.

In Terminal 2, from the repository root:

```sh
export CAREPULSE_CLIENT_SESSION=/tmp/carepulse-qm-client-session.json
cd mobile-app
flutter test qm/cross_client_test.dart --dart-define=API_BASE_URL=http://127.0.0.1:5516/api/v1 --dart-define=QM_PHASE=patient --machine
cd ../web-admin
QM_PHASE=doctor CI=true npm test -- --watchAll=false --runInBand --runTestsByPath src/qm/dispatch.client.test.js
cd ../mobile-app
flutter test qm/cross_client_test.dart --dart-define=API_BASE_URL=http://127.0.0.1:5516/api/v1 --dart-define=QM_PHASE=nurse --machine
cd ../web-admin
QM_PHASE=verify CI=true npm test -- --watchAll=false --runInBand --runTestsByPath src/qm/dispatch.client.test.js
```

Run phases in order and stop on any failure; do not blindly rerun the doctor phase after a successful assignment. Restart the harness for a fresh complete run. Windows PowerShell uses `$env:NAME = 'value'` instead of `export` or inline environment assignments; choose a private temporary file path in both terminals.

When finished, press Ctrl+C in Terminal 1 for normal disposal of the fixture database and handoff file. If the process is forcibly killed, have the local test-server owner remove that specific disposable database/file; never use a blanket database cleanup command.

Ordinary Jest runs skip the opt-in live client test when `CAREPULSE_CLIENT_SESSION` is absent. The Dart integration file is outside `mobile-app/test`, so ordinary Flutter suites remain standalone. The harness is deliberately outside the solution's ordinary CI test run.

## Results and remaining acceptance

Two Flutter phase tests and two Jest phase tests passed. Reports are in `evidence/client-integration-20261001/`. They are four executions composing one workflow, not four independent E2E workflows. Full UI/browser/emulator automation is still outside this run. Continue the app manual plan for screen/GPS acceptance, record actual observations, and retain that limitation in the final report.
