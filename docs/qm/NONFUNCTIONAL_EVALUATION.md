# Supplementary reliability and accessibility evaluation

Run: 3 October 2026. Evidence: [nonfunctional-20261003](evidence/nonfunctional-20261003/). Source hashes and base commit are in metadata.json; source included uncommitted changes. These results supplement the mandatory performance and security evaluations.

## Rationale and results

Dispatch must retain failures accurately, prevent concurrent nurse assignments and recover interrupted workflows. Staff and patients also need identifiable form controls and error messages. We selected existing xUnit/Flutter tests for these failure paths and axe-core 4.13.0 with Jest/Testing Library for rendered React form semantics.

| Check | Actual outcome | Scope |
| --- | --- | --- |
| Login form with failed-login alert | Passed; no definite axe violations or incomplete rules | Mocked authentication; checks alert presence, not audible screen-reader output |
| Patient registration | Passed; no definite violations; 14 nodes require multiple-label review | Component markup in jsdom |
| Consultation form | Passed; no definite violations; 3 nodes require multiple-label review | Synthetic booking data |
| Dispatch assignment form | Passed; no definite violations; 3 nodes require multiple-label review | Mocked store with synthetic nurse/case |
| Backend concurrency/recovery | 2 passed | Real HTTP application fixture and isolated PostgreSQL |
| Flutter dispatch session failures | 3 passed | Mocked HTTP/session responses |
| Full React regression | 37 passed, 1 skipped | Includes the four accessibility tests; opt-in HTTP harness test skipped because session configuration absent |

Axe's `form-field-multiple-labels` incomplete results are **unresolved manual checks**, not confirmed defects and not passes for that rule. Raw per-form JSON preserves affected nodes and details. Color contrast was disabled because jsdom does not provide a reliable visual rendering environment. These results do not establish WCAG conformance, keyboard usability, visual contrast, screen-reader behavior or full-page accessibility. No production fix was justified by the definite automated findings.

Backend assertions exercise one winner in concurrent assignments, release after completion, reassignment of the waiting case, forbidden-tool rejection and idempotent manual-review recovery. The interrupted state is arranged in a fixture; this was not a process-kill or database-outage experiment. Flutter checks reject an expired session and propagate telemetry 401 and completion conflict instead of reporting success; they do not prove physical-device reconnect behavior.

## Reproduction

Use a new evidence directory to preserve these original reports. Run from the repository root unless shown otherwise. The database connection must target an isolated local test server: fixtures create and drop test databases. Never use shared Neon.

```sh
mkdir -p docs/qm/evidence/nonfunctional-rerun
cd web-admin
npm ci
QM_AXE_EVIDENCE="$PWD/../docs/qm/evidence/nonfunctional-rerun" CI=true npm test -- --watchAll=false --runInBand --json --outputFile=../docs/qm/evidence/nonfunctional-rerun/web-regression.json
```

For the backend, set `CAREPULSE_TEST_CONNECTION` to your isolated local PostgreSQL connection, then run from the root:

```sh
dotnet test tests/CarePulse.Api.Tests --configuration Release --filter 'FullyQualifiedName~ConcurrentAssignments|FullyQualifiedName~AdversarialPlannerOutput' --logger trx --results-directory docs/qm/evidence/nonfunctional-rerun/reliability
```

From `mobile-app`:

```sh
flutter test test/services/dispatch_session_test.dart --machine
```

Capture stdout/stderr when rerunning. The recorded run used Node 24.11.1/npm 11.6.2, .NET 8.0.423 and Flutter 3.47.1/Dart 3.13.1 on macOS. It is local evidence, not a claim about a new GitHub Actions run. Accessibility mocks are explicit in `web-admin/src/qm/accessibility.test.js`.

## Remaining human checks

- Run login, registration, consultation and dispatch forms in a real browser using only Tab/Shift+Tab/Enter; inspect focus visibility/order and validation errors.
- Review the incomplete label nodes in each raw axe file; verify each control's announced name using a screen reader and inspect visual contrast with browser tooling.
- Execute the app-only integrated flow in APP_MANUAL_TEST_PLAN.md and record browser/device/version, date, expected versus observed results and screenshots of synthetic records.
- Test the actual intended Android device/emulator and browser combinations. Automated Flutter service tests and React jsdom results do not establish browser or physical-device compatibility.

Full UI E2E case INT-02 remains Not run. User screenshots from earlier development are useful context but are not converted into a new completed QM acceptance run without confirmed observations.
