# Student 1 (IT24101521) – Test cases, results and defect log

Area: Patient identity, medical records and document vault; Agent 1 (Planner).
Tool: xUnit 2.5.3 + Moq/fakes + EF Core InMemory, run with `dotnet test`; coverage with coverlet (`XPlat Code Coverage`).
Date run: 7 October 2026. Branch: `develop` (uncommitted at run time; commit hash to be added after commit).
Test files: `tests/CarePulse.Api.Tests/Student1/DocumentVaultSecurityTests.cs`, `tests/CarePulse.Api.Tests/Student1/PlannerSafetyTests.cs`.

## Commands (from project root)

```
dotnet test tests/CarePulse.Api.Tests --filter "FullyQualifiedName~Student1" --logger trx --results-directory docs/qm/evidence/personal/student1-run2
dotnet test tests/CarePulse.Api.Tests --filter "FullyQualifiedName~PatientServiceTests|FullyQualifiedName~AgentPlannerServiceTests|FullyQualifiedName~StaffRegistrationServiceTests|FullyQualifiedName~Student1" --collect:"XPlat Code Coverage" --results-directory docs/qm/evidence/personal/student1-coverage
```

## Test cases – document vault (DocumentVaultSecurityTests)

| ID | Feature | Type | Input / steps | Expected | Actual | Status |
|---|---|---|---|---|---|---|
| S1-DV-01 | Upload size | Invalid | 0-byte `empty.pdf` | ValidationFailed | ValidationFailed | Passed |
| S1-DV-02 | Upload size | Boundary | PDF of exactly 10,485,760 bytes | Accepted | Accepted | Passed |
| S1-DV-03 | Upload size | Boundary | PDF of 10,485,761 bytes | ValidationFailed | ValidationFailed | Passed |
| S1-DV-04 | Extension allow-list | Invalid | `.exe`, `.js`, `.html`, no extension (4 cases) | ValidationFailed | ValidationFailed | Passed |
| S1-DV-05 | Magic-byte check | Invalid | `.pdf` / `.png` / `.jpg` whose bytes start with `MZ` (3 cases) | ValidationFailed | ValidationFailed | Passed |
| S1-DV-06 | Valid types | Normal | `scan.PDF`, `scan.png`, `scan.jpeg` with correct headers | Accepted; server-chosen content type stored | As expected | Passed |
| S1-DV-07 | Authorization | Security | Patient B uploads to Patient A | Forbidden; nothing stored | Forbidden; nothing stored | Passed |
| S1-DV-08 | Authorization | Security | Doctor uploads to a patient | Forbidden (owner/Admin only) | Forbidden | Passed |
| S1-DV-09 | Authorization | Security | Patient B, owner and Doctor download one document | B Forbidden; owner and Doctor succeed | As expected | Passed |
| S1-DV-10 | Soft delete | Failure | Delete a document, then download using a new DbContext | NotFound | NotFound | Passed (after test fix, see D-S1-01) |
| S1-DV-11 | Path traversal | Security | File name `../../../evil.pdf` | Stored path stays under `uploads/patients/{id}/`, no `..` | As expected | Passed |

## Test cases – Agent 1 planner (PlannerSafetyTests, fake LLM)

| ID | Feature | Type | Input / steps | Expected | Actual | Status |
|---|---|---|---|---|---|---|
| S1-PL-01 | Prompt injection | Security | 3 injected objectives, LLM returns a valid 3-step plan | PlanCreated, ReviewStatus NotReviewed, 3 steps, objective sent inside the "untrusted" block | As expected | Passed |
| S1-PL-02 | Step order | Invalid | Valid agents in wrong order (2 cases) | ValidationFailed, no steps stored | As expected | Passed |
| S1-PL-03 | Step count | Boundary | 4 steps, all with allowed agent names | ValidationFailed | ValidationFailed | Passed |
| S1-PL-04 | Task length | Boundary | Step task of 500 characters | PlanCreated | PlanCreated | Passed |
| S1-PL-05 | Task length | Boundary | Step task of 501 characters | ValidationFailed | ValidationFailed | Passed |
| S1-PL-06 | Objective length | Invalid | `""`, whitespace, `abcd` (3 cases) | ValidationFailed; LLM not called | As expected | Passed |
| S1-PL-07 | Objective length | Boundary | `fever` (5 characters) | Accepted; LLM called once | As expected | Passed |
| S1-PL-08 | Ownership | Security | Patient B creates a plan for Patient A | Forbidden; LLM not called; no workflow saved | As expected | Passed |
| S1-PL-09 | Review scoping | Security | Review plan A using Patient B's id | NotFound | NotFound | Passed |
| S1-PL-10 | Approval enforcement | Failure | Review the same plan twice | Second review Conflict | Conflict | Passed |

## Results

| Run | Scope | Passed | Failed | Evidence |
|---|---|---|---|---|
| student1-run1 | New Student 1 tests (first run) | 32 | 1 (S1-DV-10) | `docs/qm/evidence/personal/student1-run1/*.trx` |
| student1-run2 | New Student 1 tests (retest) | 33 | 0 | `docs/qm/evidence/personal/student1-run2/*.trx` |
| student1-coverage | PatientService, AgentPlannerService, StaffRegistration and Student1 tests combined | 89 | 0 | `docs/qm/evidence/personal/student1-coverage/**/coverage.cobertura.xml` |

Line coverage from the cobertura report: `PatientService` 97.1%, `AgentPlannerService` 95.3%. Whole-assembly coverage is only 9.3% because this run excludes the other students' areas, so do not quote that figure as a system-wide result.

## Defect log

| ID | Description | Severity | Steps to reproduce | Cause | Fix | Retest |
|---|---|---|---|---|---|---|
| D-S1-01 | S1-DV-10 failed on first run: a soft-deleted document was still returned by `GetDocumentFileAsync`. | Low (test defect, not a product defect) | Run S1-DV-10 as first written (upload, delete and download all on one DbContext). | The same DbContext still tracked the deleted entity, so EF returned it from its identity map. The global soft-delete query filter (`CarePulseDbContext.cs:211`) was never applied. Each real HTTP request gets a new context, so production behaviour is correct. | Test changed to download using a second DbContext over the same in-memory database. No production code changed. | student1-run2: passed. |

| D-S1-02 | Web registration form rejected an email or NIC with leading/trailing spaces (for example a pasted value), while the mobile validators trim and accept the same input. Cross-platform inconsistency. | Low–Medium (usability, consistency) | Register a patient on the web with email `" a@b.com "` or NIC `" 199012345V "`. | `RegisterPatientPage.js` validated the raw `form` values and sent them untrimmed; `validators.dart` trims first. | Trim email, NIC and phone before validating, and send trimmed email, full name, NIC and phone in the payload (`RegisterPatientPage.js`, 7 lines changed). | student1-web-run2: 1 failed (before fix). student1-web-run3: 37 passed, 0 failed (after fix). |
| D-S1-03 | Two of the first-run web tests failed because of my own test code: contact name input located by label (it only has a placeholder), and the button re-queried by its old name after the label changed to "Saving…". | Test defects only | n/a | n/a | Test code corrected. | Passed in student1-web-run2. |

| D-S1-04 | HTTP run1: Doctor/Nurse requests returned 401 instead of 200/403/404. | Test defect | Run run1 of PatientsApiSecurityTests. | The API deliberately rejects staff tokens with no Doctor/Nurse profile row. My test users had none. | Seeded `DoctorProfile` and `NurseProfiles` rows; added S1-API-03 to assert the deactivated-account behaviour. | run2: 1 failure left; run3: 49 passed. |
| D-S1-05 | HTTP run2: Doctor POST to `/documents` returned 415 instead of 403. | Test defect (framework ordering, not a bypass) | Send a JSON body to the upload endpoint. | The endpoint only accepts `multipart/form-data`; routing answers 415 before authorization. | Test sends multipart content for that endpoint. | run3: passed. |

D-S1-01, D-S1-03, D-S1-04 and D-S1-05 are test defects. D-S1-02 is the only product defect found so far, and the only one to present as a product bug.

## Test cases – HTTP authentication and authorization (PatientsApiSecurityTests)

File: `tests/CarePulse.Api.Tests/Student1/PatientsApiSecurityTests.cs`. Tool: xUnit + `WebApplicationFactory<Program>` (real JWT middleware, `[Authorize(Roles)]` and routing; in-memory database). 49 cases, run command:
`dotnet test tests/CarePulse.Api.Tests --filter "FullyQualifiedName~PatientsApiSecurityTests" --logger trx --results-directory docs/qm/evidence/personal/student1-http-run3`

| ID | Feature | Type | Input | Expected | Status |
|---|---|---|---|---|---|
| S1-API-01 | Authentication | Security | 14 endpoints of `/api/v1/patients` with no token | 401 each | Passed |
| S1-API-02 | Token integrity | Security | Tampered signature; token signed with another secret; expired token; garbage bearer value (4 cases) | 401 each | Passed |
| S1-API-03 | Deactivated staff | Security | Valid Doctor token but no DoctorProfile row | 401 (`Program.cs` "account no longer active" check) | Passed |
| S1-API-04 | Role authorization | Security | 13 wrong-role calls (Patient/Nurse on staff endpoints, Doctor on patient-only and admin-only endpoints) | 403 each | Passed |
| S1-API-05 | IDOR | Security | Patient B reads Patient A's profile, history, contacts, documents, and triggers an emergency alert for A | 403 each | Passed |
| S1-API-06 | Positive control | Normal | Patient A reads own profile; Doctor reads any profile | 200 | Passed |
| S1-API-07 | Routing | Invalid | Unknown profile GUID; non-GUID id | 404 | Passed |
| S1-API-08 | Hostile query strings | Invalid / boundary | SQL-injection text, `<script>`, `page=-1`, `pageSize=0`, `pageSize=2147483647`, `minAge>maxAge`, bogus status/sort (9 cases) | Never 5xx | Passed |

Runs: student1-http-run1 (48 tests): 10 failed, all caused by test setup (Doctor/Nurse tokens need a profile row; see D-S1-04). student1-http-run2: 1 failed (documents endpoint answers 415 to a JSON body; see D-S1-05). student1-http-run3: 49 passed, 0 failed.

Limitation: the in-memory provider cannot prove real SQL-injection safety (EF parameterises queries; the OWASP ZAP scan is the stronger evidence); this check only proves the API does not crash on hostile input.

## Integrated end-to-end workflow (PatientWorkflowE2ETests) – report ID INT-02

File: `tests/CarePulse.Api.Tests/Student1/PatientWorkflowE2ETests.cs`. Tool: xUnit + `WebApplicationFactory<Program>`; real routing, JWT login, role authorization, services, EF Core (in-memory) and Planner agent. Only the LLM (`FakeAiPlannerClient`) and the SMS gateway (`FakeNotificationService`) are replaced.
Command: `dotnet test tests/CarePulse.Api.Tests --filter "FullyQualifiedName~PatientWorkflowE2ETests" --logger trx --results-directory docs/qm/evidence/personal/student1-e2e-run1`

Steps checked in one run: (1) Admin registers a patient (201); (2) patient logs in and reads own profile; (3) patient adds an emergency contact and a medical history entry; (4) patient creates an AI plan (PlanCreated, 3 steps, NotReviewed); (5) patient cannot approve their own plan (403); (6) Doctor approves and the patient sees Approved; (7) patient triggers an emergency alert (one SMS to the contact, blood group captured); (8) Doctor reads the alert log and audit trail; (9) another patient is refused (403) on the profile and the plans.

Result: 1 test, passed. Sanity check: changing one expected value (3 steps to 4) made the test fail, then it was restored. Total Student 1 backend run with this test: 83 passed (`--filter "FullyQualifiedName~Student1"`). Overall Student 1 total is now 133 (33 + 49 + 1 + 21 + 29).

Limitation: in-memory database and fake AI and SMS; it does not exercise the web or mobile screens or a real PostgreSQL server.

## Test cases – web registration form (RegisterPatientPage.boundary.test.js, Jest + React Testing Library)

File: `web-admin/src/pages/RegisterPatientPage.boundary.test.js`. 21 cases.

| ID | Feature | Type | Input | Expected | Status |
|---|---|---|---|---|---|
| S1-WEB-01 | Password | Boundary | 7 vs 8 characters | 7 rejected with message and no API call; 8 accepted | Passed |
| S1-WEB-02 | Date of birth | Boundary | today / tomorrow | today accepted; tomorrow rejected | Passed |
| S1-WEB-03 | National ID | Invalid | 11, 13 digits, 12 digits + letter, 9 digits no suffix, 10 digits + V, empty (6 cases) | Rejected; no API call | Passed |
| S1-WEB-04 | National ID | Normal | 9 digits + lowercase x; 12 digits | Accepted | Passed |
| S1-WEB-05 | Phone | Invalid | 9 digits, 11 digits, no leading 0, letters (4 cases) | Rejected; no API call | Passed |
| S1-WEB-06 | Emergency contact | Invalid | Row with only a name | Blocked with message | Passed |
| S1-WEB-07 | Required fields | Invalid | Submit empty form | One error per required field; no API call | Passed |
| S1-WEB-08 | Double submit | Failure | Click twice while request is pending | Button disabled; API called once | Passed |
| S1-WEB-09 | Network failure | Failure | API rejects with no response body | Alert shown; stays on form | Passed |
| S1-WEB-10 | Whitespace | Normal/consistency | Email and NIC padded with spaces | Accepted and trimmed payload | Failed before fix (D-S1-02), Passed after |

Web result: student1-web-run3 (RegisterPatientPage, ProtectedRoute, LoginPage, PatientsList, PatientDetail suites): 6 suites, 37 tests, 0 failed. RegisterPatientPage.js line coverage was about 80% (statements) in that run.

## Test cases – Flutter validators (validators_boundary_test.dart, flutter_test)

File: `mobile-app/test/validators_boundary_test.dart`. 29 cases run (`flutter test test/validators_boundary_test.dart --reporter expanded`), 29 passed, 0 failed. Evidence: `docs/qm/evidence/personal/student1-flutter-run1/results.log`.

Covers national ID (11/13 digits, letter endings, SQL-style string, null, trimming), phone (9/11 digits, letters, `+94` prefix, trimming), password (7 vs 8 characters, null, empty), email (no dot, whitespace, null, empty, trimming), date of birth (today, tomorrow, 1900) and `requiredField`.

Trimming behaviour here is the reference that exposed D-S1-02.

## Non-functional testing

### Performance – k6 (read-only load test)

- Tool: k6 v2.3.0 (portable build). Script: `docs/qm/student1/perf/patients-load.js`. Credentials are passed with `-e QM_EMAIL=... -e QM_PASSWORD=...` and never stored in the file.
- Target: local API (`http://localhost:5014`) connected to the shared Neon PostgreSQL database, so latency includes the round trip to the cloud database.
- Load profile: ramp 0 to 5 users in 20 s, hold 10 users for 40 s, ramp down in 10 s (about 70 s). Each iteration: `GET /api/v1/patients?page=1&pageSize=10`, `GET /api/v1/patients?search=a&sortBy=fullName`, and an unauthenticated `GET /api/v1/patients` that must return 401 (negative control).
- Thresholds: `http_req_failed < 1%`, p(95) of list and search < 1500 ms, checks > 99%.
- Command: `k6 run --summary-export docs/qm/evidence/personal/student1-k6/summary.json -e QM_EMAIL=... -e QM_PASSWORD=... docs/qm/student1/perf/patients-load.js`
- Evidence: `docs/qm/evidence/personal/student1-k6/summary.json` and `k6-output.txt`.

| Metric | Result |
|---|---|
| Total requests / throughput | 715 requests, about 10.1 requests/s, 238 iterations, max 10 virtual users |
| Failed requests | 0.00% (0 of 715) |
| Checks | 100% (715 of 715): login ok, list 200, search 200, anonymous 401 |
| List endpoint | avg 330 ms, p(95) 361 ms, max 912 ms |
| Search endpoint | avg 315 ms, p(95) 344 ms, max 711 ms |
| Thresholds | All passed |

Interpretation: at 10 concurrent users the patient registry stays well under the 1.5 s target with no errors, and anonymous requests are still rejected correctly under load. The first smoke run showed `http_req_failed` at 31%; that was a script issue (k6 counts the expected 401 as a failure) and was fixed with `http.expectedStatuses(401)`; it is not a system defect. Limitations: one run, 10 users only, no stress or spike test (the database is shared with other students), local API against a cloud database, so absolute numbers will differ in production.

### Security – OWASP ZAP baseline (passive only)

- Tool: OWASP ZAP 2.17.0 core in daemon mode, with the OpenAPI add-on. Evidence folder: `docs/qm/evidence/personal/student1-zap/` (`before-fix-report.html`, `before-fix-alerts.json`, `after-fix-report.html`, `after-fix-alerts.json`).
- Method, deliberately passive: (1) imported `/swagger/v1/swagger.json` with no token, so every protected call was answered 401 (checked in the API log: no write call succeeded; only the unauthenticated `register` and `login` calls were answered 400); (2) sent about 16 read-only authenticated GET requests (patient list, search, audit log, emergency alerts, profile, history, contacts, documents, unknown routes, a SQL-style search string) through ZAP as a proxy with an Admin token; (3) read ZAP's passive-scan alerts. No active scan, no write calls.
- A CORS probe with a disallowed origin (`http://evil.example`) returned no `Access-Control-Allow-*` headers, which is the correct result.

| Alert | Risk | Where | Triage | Result |
|---|---|---|---|---|
| X-Content-Type-Options header missing | Low (13 URLs) | Every API response | Real issue | Fixed (D-S1-06) |
| CSP header not set | Medium (1 URL) | `/swagger/index.html` | Swagger UI is development-only; a strict CSP would break it | Accepted risk |
| Missing anti-clickjacking header | Medium (1 URL) | `/swagger/index.html` | Same as above | Accepted risk |
| Sensitive information in URL | Informational | `/patients/audit-log?userId=...` | False positive: ZAP matched the parameter name `userId`; the value is a filter, not a secret | Not an issue |
| Modern web application | Informational | Swagger UI | Informational only | None |

Summary: before the fix 0 High, 2 Medium, 13 Low, 2 Informational. After the fix 0 High, 2 Medium (Swagger only), 0 Low, 2 Informational.

Limitations: passive scan only, so injection and authentication weaknesses are not actively probed; the authenticated requests were a hand-picked set, not a full crawl; HSTS and HTTPS were not assessed because the test ran over local HTTP.

| ID | Description | Severity | Cause | Fix | Retest |
|---|---|---|---|---|---|
| D-S1-06 | API responses carried no security headers (`X-Content-Type-Options`, clickjacking and CSP protection), found by the ZAP baseline. | Low | No middleware set any security headers. | `Program.cs`: small middleware adds `X-Content-Type-Options: nosniff` and `Referrer-Policy: no-referrer` to all responses, plus `X-Frame-Options: DENY` and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` for everything except `/swagger`. | `/health` response shows all four headers; ZAP rescan: Low alerts 13 to 0; 124 backend tests still pass. |

## Limitations (be ready to explain)

- EF Core InMemory does not enforce relational constraints or the Postgres-specific behaviour. These are service-level tests, not database tests.
- The LLM is replaced by `FakeAiPlannerClient`, so these tests check the validator and the safeguards, not live Gemini behaviour.
- Controller `[Authorize(Roles=...)]` attributes are not exercised here. Role checks tested are those inside the service. HTTP-level 401/403 testing needs `WebApplicationFactory` or Postman.
- `ReviewPlanAsync` has no role check in the service; role enforcement is the controller's job. This is untested here.

## Contribution declaration (complete before submitting)

- Written with AI assistance (Claude Code). I must read, understand and be able to re-run and modify every test above before the viva.
- Items still to do by me: reading the code, adding my own variation, committing under my own git identity, and recording the final commit hash.
