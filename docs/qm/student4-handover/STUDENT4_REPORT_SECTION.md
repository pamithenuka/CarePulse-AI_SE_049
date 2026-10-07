# Student 4 - Dispatch testing contribution

Name: Kaluarachchige P.E.K | Student ID: IT24100966

Project: CarePulse | Testing date: 6 October 2026

## Scope and purpose

My area was nurse dispatch, completion, session failures and dispatch safety rules. The tests checked approval and ownership, saved workflow results, concurrent nurse assignment, authentication failures and safety warnings. I personally ran the selected tests and applied two AI-assisted test extensions.

## Tools and environment

Backend tests used .NET 8, xUnit, the API test host and an isolated PostgreSQL 18 Docker container named carepulse-qm-viva on localhost port 55442. Flutter service tests used flutter_test, a mock HTTP client and mocked secure storage. Safety tests used xUnit and an in-memory database without a Gemini key. These runs did not test the deployed Render system or shared Neon database.

## Results

| Selected suite | Latest personal result |
| --- | --- |
| Flutter dispatch session and error handling | 4 passed, 0 failed |
| Backend workflow and nurse concurrency | 2 passed, 0 failed, 0 skipped |
| Dispatch safety boundaries and fallback | 8 passed, 0 failed, 0 skipped |

Total: 14 distinct selected test cases passed across separate runs. This is not a fresh full-project suite result. Earlier runs of 3 Flutter and 7 safety cases are superseded by the extended runs; do not add them again to the total.

## What the tests checked

Flutter: expired tokens are rejected; location requests include the shared JWT and propagate a server 401 error; a completion conflict returns an error; and an expired session prevents a location request from reaching the simulated server.

Backend: the integrated workflow checks approval, ownership and durable completion. The concurrency test checks that one nurse cannot receive two active assignments and that completing the first assignment allows the waiting case to proceed.

Safety: six parameterized cases check severity and ETA warning boundaries. Two other cases check fallback behavior when AI configuration is missing, including continued human approval. The configured warning limits are severity above 8.5 and ETA above 30 minutes; these tests verify application rules, not clinical standards.

## My changes and execution

1. I added [InlineData(8.49, 31, false, true)] to ValidationAgentGoldenTests.cs following Codex guidance. It expects only an ETA warning because severity is below its limit while ETA exceeds its limit. The existing assertions also check the exact flag count and human approval. The extended safety run passed all 8 cases.

2. I applied the Codex-provided Flutter test named 'expired session prevents a location request from being sent'. It calls the real updateLocation service with an expired mocked session, checks for a 401 exception and verifies that the mock server received zero requests. This extends the earlier token-only check. The extended Flutter run passed all 4 cases.

I executed existing backend tests as well. I do not claim independent authorship of the existing suites or the AI-generated test code.

## Defects and limitations

No product defect was found in these personal runs, so no personal bug fix is claimed. The reruns verified the test extensions. An EF1002 build warning in a separate migration test concerned fixed table-name interpolation; the two selected backend tests passed. It is not recorded as a dispatch defect.

These tests do not verify physical GPS, real mobile screens, live Gemini, production performance or deployed security. Group performance/security evidence should be described separately and not attributed to my personal execution. Passing these selected cases does not establish that the whole system is fault-free.

## AI assistance declaration

OpenAI Codex helped select and explain tests and commands, suggested the safety data row, generated the additional Flutter test, and helped prepare this documentation. I applied the changes and personally ran the commands. Results were checked against saved logs and TRX files. This records actual assistance; any module-specific CLEAR formatting should be applied when available. Viva preparation is deferred and is not claimed as completed here.

## Evidence and report handover

The accompanying ZIP contains this section, its PDF, original personal result files, and snapshots of the three relevant test source files. Use the latest Flutter extended-results.log, backend student4-run1 TRX and safety student4-safety-run2 TRX for the 14-case summary. The earlier logs are retained for traceability.

The source snapshots were collected after the runs. The current base commit at handover is 48cf4641b7520e3882050abc2d26178d3652e1a8; testing changes are uncommitted, so this hash alone does not identify the tested source. Exact run-time commit metadata was not captured. Add the final testing commit/PR link after committing; no commit or push is claimed by this handover.

Conclusion: the selected personal test executions and evidence collection are complete for this scope. The report author can incorporate this section now. Final Git contribution evidence, the team submission checks and individual viva preparation remain separate tasks.
