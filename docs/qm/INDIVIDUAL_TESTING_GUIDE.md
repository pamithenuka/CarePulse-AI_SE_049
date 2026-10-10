# Each member's testing and viva guide

The automated group evidence is already available. Each member now needs to personally run, understand and explain a meaningful test. Running a command alone is not proof that you wrote the test. Record any changes you actually make and declare AI assistance.

Names have not yet been mapped to Students 1–4. Choose the section matching your project responsibility, then fill in CONTRIBUTION_RECORD.md. Do not repeat every group test.

## 1. Prepare once on your laptop

Pull the team's agreed branch after the testing files have been committed and merged. Do not discard local changes to pull. Follow README.md for .NET 8 setup. From the project root:

```sh
dotnet tool restore
dotnet restore CarePulse.sln
```

These automated tests need their own local PostgreSQL server. **Do not copy the Neon connection into CAREPULSE_TEST_CONNECTION.** The fixture creates and deletes temporary databases.

An easy option, if Docker Desktop is installed and running, is this separate test container:

```sh
docker run --name carepulse-qm-viva -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=qm-local-only -e POSTGRES_DB=postgres -p 127.0.0.1:55442:5432 -d postgres:18
docker exec carepulse-qm-viva pg_isready -U postgres -d postgres
```

Wait until the second command says it is accepting connections. If this container already exists, use `docker start carepulse-qm-viva` instead of creating it again. If port 55442 is occupied, use another free local port and update the connection below. This setup recipe has not been executed as part of the recorded results; earlier runs used a native local PostgreSQL server.

In the same terminal used for tests, set the connection.

macOS/Linux:

```sh
export CAREPULSE_TEST_CONNECTION='Host=127.0.0.1;Port=55442;Database=postgres;Username=postgres;Password=qm-local-only'
```

Windows PowerShell:

```powershell
$env:CAREPULSE_TEST_CONNECTION='Host=127.0.0.1;Port=55442;Database=postgres;Username=postgres;Password=qm-local-only'
```

This password is only for the disposable local container. The web app, mobile app and normal API do not need to run for these xUnit tests. The test host creates synthetic accounts and substitutes AI, so these demonstrations need no Gemini key.

## 2. Student 1 — patient records and security

Open `tests/CarePulse.Api.Tests/IntegratedWorkflowTests.cs`. Read `Security_CrossPatientUpload_IsRejectedWithoutDocument` and `Security_InvalidUpload_IsRejectedWithoutDocument`.

```sh
dotnet test tests/CarePulse.Api.Tests --configuration Release --filter "FullyQualifiedName~Security_CrossPatientUpload|FullyQualifiedName~Security_InvalidUpload" --logger trx --results-directory docs/qm/evidence/personal/student1-run1
```

Expected: 4 tests pass. A different patient cannot upload to the owner's record (403). HTML, a fake PNG and an empty PDF are rejected (400). The tests also check that no document row was saved.

Explain why checking the database matters: an error response alone would not prove that the invalid upload left no record.

Practice a small change: add another clearly invalid HTML filename/body as an InlineData case. Predict the result, run it and explain it. Save your own result; do not count this practice as done before doing it.

## 3. Student 2 — AI validation

Open `tests/CarePulse.Api.Tests/Services/AgentPlannerServiceTests.cs`. Read the valid response, malformed JSON, disallowed agent and incomplete delegation tests.

```sh
dotnet test tests/CarePulse.Api.Tests --configuration Release --filter "FullyQualifiedName~CreatePlanAsync_ValidLlmResponse|FullyQualifiedName~CreatePlanAsync_MalformedJson|FullyQualifiedName~CreatePlanAsync_DisallowedAgentName|FullyQualifiedName~CreatePlanAsync_IncompleteDelegation" --logger trx --results-directory docs/qm/evidence/personal/student2-run1
```

Explain the actual passed count shown by your runner; parameterized cases can produce more than one result per method. A valid plan should be stored. Invalid JSON, unsupported agents and missing steps should fail safely.

Explain that these tests use controlled AI responses, not live Gemini. Read AI_EVALUATION.md and QM-AI-001 in DEFECT_REPORT.md to explain the separate live sample and why correct JSON can still contain unsuitable instructions.

Practice changing a fake output to another unsupported agent name, then rerun the matching test. Keep the rejection assertion; do not weaken it to make a test pass.

## 4. Student 3 — appointments and database integrity

Open `tests/CarePulse.Api.Tests/AppointmentBookingTests.cs`.

```sh
dotnet test tests/CarePulse.Api.Tests --configuration Release --filter "FullyQualifiedName~AppointmentBookingTests" --logger trx --results-directory docs/qm/evidence/personal/student3-run1
```

Expected: 3 tests pass. An open slot can be booked. A booked slot returns a conflict. Two database contexts that saw the same open slot cannot both book it successfully.

Be precise: the last test prepares two contexts before booking, then calls the bookings in sequence. It checks a stale-read conflict; it does not launch both calls at exactly the same instant.

Also read PERFORMANCE_EVALUATION.md. Explain the dataset, five-client target and p95 result. Only claim that you personally ran the performance script if you follow that guide and save a new run.

Practice changing a synthetic fixture value that does not alter the booking rule, then explain why the expected one-success/one-conflict result stays the same.

## 5. Student 4 — dispatch, approval and recovery

Open `tests/CarePulse.Api.Tests/IntegratedWorkflowTests.cs`. Read the complete workflow and nurse concurrency tests.

```sh
dotnet test tests/CarePulse.Api.Tests --configuration Release --filter "FullyQualifiedName~GoldenWorkflow_ExecutesAllAgents|FullyQualifiedName~ConcurrentAssignments_ClaimNurseOnce" --logger trx --results-directory docs/qm/evidence/personal/student4-run1
```

Expected: 2 tests pass. The workflow checks approval, ownership and saved completion. The assignment test checks that one nurse cannot serve two active assignments; completing the first case lets the waiting case be assigned.

Explain which assertions prove the final result is stored and which prevent another nurse from updating the case. Explain that AI is substituted and this is HTTP/database testing, not phone GPS testing.

For a client failure demonstration, run from `mobile-app` after `flutter pub get`:

```sh
flutter test test/services/dispatch_session_test.dart
```

Expected: 3 pass. Expired sessions and rejected telemetry/completion are not shown as success. Save the terminal output yourself.

## 6. Save your actual contribution

1. Use your own fresh results directory for each run; change `run1` to `run2` for a repeat.
2. Record the date, source commit (`git rev-parse HEAD`), test/filter, actual passed/failed count and evidence path in CONTRIBUTION_RECORD.md.
3. Take a screenshot of the test command and result if useful. Do not capture private settings or tokens.
4. If you change a test, record what and why. Keep a real commit or diff as evidence. Do not change application rules merely to force a pass.
5. If a test fails, save the failure, investigate it and save a separate retest. Never replace expected results with the observed failure.
6. Explain the input, expected behavior, assertion, tool choice and limitation without reading a script word for word.

An unavailable database or missing environment variable is a setup failure, not a product defect. A test reporting zero matching cases is not a successful demonstration.

Stop the dedicated container when finished:

```sh
docker stop carepulse-qm-viva
```

Do not delete shared databases. For a group demonstration of the cross-client workflow, follow CLIENT_INTEGRATION.md in order, using your isolated local connection. Follow APP_MANUAL_TEST_PLAN.md separately for real screens; automated service tests do not replace those observations.
