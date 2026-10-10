# Assertion review and targeted gap tests

1 October 2026. This review closes selected high-priority gaps; it is not a claim that all possible paths are tested.

## Findings and changes

| Gap | Why it matters | Added evidence |
| --- | --- | --- |
| Future consultation rejection was described in manual tests but absent from consultation automation | Early completion can create an inaccurate visit record | Future booking rejected; fresh database context confirms no consultation and booking still intact |
| Consultation note/prescription boundaries were untested | Empty or oversized input could bypass limits, or valid maximum input could be rejected | Four cases: empty notes, accepted 4000/2000 limits, rejected 4001 notes, rejected 2001 prescription; persisted content and identities checked |
| Request doctor/patient could differ from booking without an explicit regression test | Wrong patient/doctor attribution corrupts records | Two mismatch cases rejected with no consultation persisted |
| Safety thresholds had only broadly high/low examples | Off-by-one threshold changes alter warnings | Five cases just below, exactly at and above severity 8.5 / ETA 30; doctor approval asserted for every result |
| Concurrent nurse test proved winner retry, but not waiting-case assignment after release | A waiting case might remain stranded even when a nurse becomes available | Existing HTTP test extended through location, arrival, completion, release, second assignment and waiting-queue removal |

## Boundaries of these tests

Consultation tests call the controller with a test identity and real PostgreSQL. They test controller/business/persistence behavior, not ASP.NET model-binding or authorization middleware. Existing HTTP workflow tests separately exercise role/ownership restrictions. Safety threshold tests deliberately disable live AI and use an in-memory context; they validate deterministic fallback, not model reasoning or database connectivity. The nurse reassignment scenario uses HTTP requests, real local PostgreSQL and fake external AI.

The integrated workflow asserts persisted workflow markers, approval, ownership, one vitals record, nurse availability and patient completion. It does not click the actual React/Flutter UI, simulate a live LLM, or establish clinical correctness. Full client-spanning and live AI evidence remain separate phases.

No production defect was discovered in these new cases. They passed on the current implementation; the identified defects were in **test coverage**, so no fabricated application fix/retest claim is made.

## Results

Run `gap-tests-20261001`: **121 API tests + 7 safety tests = 128 passed**, zero failures/skips. Twelve new parameterized test cases were added and one existing workflow test strengthened. See the TRX reports and backend log under `evidence/gap-tests-20261001/`.

Baseline remains preserved: 116 backend tests passed before these additions. Web and Flutter code did not change and were not rerun in this step. The earlier 90.09% line coverage is a baseline measurement, not a new coverage result for this run.

## Remaining gaps assigned to following phases

- Full client-spanning workflow, including live API communication and client error states.
- Live AI task/agent/tool evaluation and documented malicious-input expectations.
- Declared seeded performance workload and measurements.
- Dedicated security probes/scan scope, upload/input coverage and finding triage.
- Scoped accessibility/compatibility and manual device observations.
- Final per-case traceability review; broad suite-level rows must not imply tests that do not exist.
