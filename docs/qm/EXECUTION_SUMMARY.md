# Test execution summary

Updated 3 October 2026. The table below is the original 1 October baseline at commit `1c638e6`; later scoped runs follow. See SOFTWARE_TESTING_REPORT.md for the consolidated report. Do not add overlapping runs as unique tests.

| Layer | Executed | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| Backend API/service/database/integration | 114 | 114 | 0 | 0 |
| Backend safety golden tests | 2 | 2 | 0 | 0 |
| React | 33 | 33 | 0 | 0 |
| Flutter visible tests | 39 | 39 | 0 | 0 |
| Total framework test cases | 188 | 188 | 0 | 0 |

Web production build also passed; build success is not counted as a test case. Parameterized framework cases differ from scenario-group rows in TEST_CASES.csv. Evidence and reproduction instructions: [EVIDENCE_INDEX.md](EVIDENCE_INDEX.md).

No new failing test was found in this baseline. Do not invent a new defect to populate the defect report. Historical defects still require truthful attribution and available retest evidence.

Current outstanding work: confirmed team details and personal contribution evidence, CLEAR declaration, manual accessibility/device checks and full UI acceptance. Report PDF is prepared for team review. Baseline tests use mocked providers; later live planner evidence is described below. No clinical accuracy claim is made.

## Follow-up: targeted gap tests

Run `gap-tests-20261001` passed **128 backend cases (121 API + 7 safety)** with zero failures/skips. This supersedes the backend count above for the expanded test source; the original baseline is preserved. Twelve cases were added and the waiting-case reassignment assertions strengthened. Web/Flutter source was unchanged and those layers were not rerun in this follow-up. Do not sum the two backend runs as unique tests. No new production defect was found; outstanding assignment scopes remain listed above.

## Follow-up: cross-client integration

Four ordered phase tests passed in one cross-client workflow: Flutter patient submission → React approval/assignment → Flutter nurse completion/patient status → React completion/nurse availability. Real production client service/store code, HTTP middleware and local PostgreSQL were used. AI and storage were substituted; actual screen interactions remain untested by this harness. Initial sandbox network EPERM was resolved by allowing loopback access and is classified as an environment issue. See CLIENT_INTEGRATION.md. Do not count this as completed full UI/device E2E acceptance.

## Follow-up: AI evaluation (3 October)

Forty selected deterministic cases passed before and after the prompt correction. Three initial live planner outputs passed narrow automatic checks but exposed unsupported capability suggestions on review. Defect QM-AI-001 was corrected in the prompt; the same three-case live retest passed structural checks and AI-assisted capability/approval review. These are bounded planner observations, not a whole-system clinical-quality or model-safety certification. See AI_EVALUATION.md and DEFECT_REPORT.md. Performance/security evidence was collected in the subsequent runs below.

## Follow-up: performance evaluation (3 October)

Seeded local workload completed 2,400 measured authenticated reads with zero errors and verified response content. Five-client p95 was 9.921 ms, passing the predeclared <1000 ms / <1% error target. Twenty-client exploratory p95 was 30.684 ms. Read-only short local bursts exclude live AI, writes, WAN and full UI; they do not establish a sustained capacity limit. See PERFORMANCE_EVALUATION.md for the dataset, warm-up, durations, throughput and raw evidence. These HTTP samples are not added to the unique framework test-case count.

## Follow-up: security evaluation (3 October)

Targeted xUnit HTTP security run passed 17 cases, including 11 new probes for unauthenticated requests, JWT tampering, role/ownership violations, invalid uploads and malformed coordinate input. Denied writes had no persisted side effects. No new application vulnerability was demonstrated; the compiler's migration-fixture warning was triaged separately. This is application-specific tool-based security evidence, not an external vulnerability scan. See SECURITY_EVALUATION.md for remaining unassessed controls. Do not sum overlapping workflow runs as unique test cases.

## Follow-up: supplementary non-functional checks (3 October)

Four axe-core component tests passed with zero definite violations; multiple-label checks on three forms remain incomplete and contrast was disabled. Full React regression passed 37 tests with one opt-in harness test skipped; the four axe tests are included in 37. Fresh backend reliability checks passed 2 tests and Flutter session checks passed 3. These overlap earlier suite coverage and must not be added as unique new cases. Raw JSON/TRX/logs and source hashes are preserved in evidence/nonfunctional-20261003. See [NONFUNCTIONAL_EVALUATION.md](NONFUNCTIONAL_EVALUATION.md) for reproduction, limitations and pending manual checks. No full UI/device or WCAG conformance claim.
