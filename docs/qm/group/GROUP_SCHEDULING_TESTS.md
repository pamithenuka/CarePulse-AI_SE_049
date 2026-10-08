# Group tests: scheduling and consultation area

These are **group tests**. They are not an individual student's contribution, and they must not be used as any student's "personally written" tests in the individual records or the viva. They were prepared by Student 1 (IT24101521) for the group, with AI assistance (Claude Code), and run on 8 October 2026. They cover the scheduling area (doctor roster, slots, booking, consultations, Agent 3 search endpoint) where the area owner's own tests are listed separately.

## What was added

| Area | File | Tool | Cases | Result |
|---|---|---|---|---|
| API security, validation and safe failure | `tests/CarePulse.Api.Tests/Group/SchedulingApiSecurityTests.cs` | xUnit + WebApplicationFactory | 53 | 53 passed |
| Integrated scheduling workflow | `tests/CarePulse.Api.Tests/Group/SchedulingWorkflowE2ETests.cs` | xUnit + WebApplicationFactory | 1 | passed |
| Shared test host (helper, no tests) | `tests/CarePulse.Api.Tests/Group/SchedulingApiTestBase.cs` | | | |
| Web scheduling pages | `web-admin/src/pages/SchedulingPages.group.test.js` | Jest + React Testing Library | 11 | 11 passed |
| Flutter data models | `mobile-app/test/models/scheduling_models_group_test.dart` | flutter_test | 16 | 16 passed |

Total: 81 cases, 81 passed, 0 failed.

Evidence: `docs/qm/evidence/group-scheduling/` (backend TRX, web and Flutter logs; the TRX and log files are git-ignored).

## Commands
```
dotnet test tests/CarePulse.Api.Tests --filter "FullyQualifiedName~Group" --logger "console;verbosity=normal"
cd web-admin ; $env:CI="true" ; npx.cmd react-scripts test --watchAll=false --testPathPattern "SchedulingPages.group" --verbose
cd mobile-app ; flutter test test/models/scheduling_models_group_test.dart --reporter expanded
```

## Backend security and validation tests (53 cases)

| ID | What is checked | Expected | Result |
|---|---|---|---|
| G-SCH-01 | 10 scheduling endpoints with no token | 401 each | Passed |
| G-SCH-02 | Patient calls roster update, slot generation, consultation completion, booked list | 403 each | Passed |
| G-SCH-03 | Doctor B changes or generates slots for Doctor A; reads A's booked list; completes A's consultation | 403 each | Passed |
| G-SCH-04 | Patient B books for Patient A | 403 | Passed |
| G-SCH-05 | Book an unknown slot; unknown patient; a past slot; an empty body | 404, 404, 409, 4xx (never 5xx) | Passed |
| G-SCH-06 | Read a consultation as its patient, its doctor, another patient, another doctor; unknown id | 200, 200, 403, 403; 404 | Passed |
| G-SCH-07 | Slot duration 4, 5, 240, 241 minutes | 400, 200, 200, 400 | Passed |
| G-SCH-08 | Roster with zero length, shorter than one slot, end before start; exactly one slot; undefined weekday; unknown doctor | 400, 400, 400; 200; 400; 404 | Passed |
| G-SCH-09 | Generate slots with no roster; end before start; unknown doctor | 400, 400, 404 | Passed |
| G-SCH-10 | Generate slots for a 90-day vs 91-day range | 200 vs 400 | Passed |
| G-SCH-11 | Consultation notes of 0, 1, 4000 and 4001 characters | 400, 200, 200, 400 | Passed |
| G-SCH-12 | Prescription of 2000 and 2001 characters | 200, 400 | Passed |
| G-SCH-13 | Consultation for a never-booked slot, a future slot, the wrong patient, an unknown slot | 400, 400, 400, 404 | Passed |
| G-SCH-14 | Slot search with an invalid date, far-future date, SQL-style text, script text, bad GUID, impossible date (6 cases) | Never a 5xx | Passed |
| G-SCH-15 | Agent 3 search with an empty or whitespace message | 400 | Passed |
| G-SCH-16 | Agent 3 search with a "book every slot" injection message and no AI key | 502 (clear failure); no slot is booked | Passed |

## Integrated workflow (G-SCH-INT, report ID INT-03)

One test through the real HTTP pipeline: the doctor sets a 7-day roster (09:00-11:00, 30-minute slots); slots are generated for 3 days (12 created; a repeat creates 0); patient A finds and books a slot; patient B and patient A cannot book it again (409) and the open list drops from 12 to 11; the doctor sees the booking; completing the consultation before the start time is refused (400); after the slot start moves into the past the doctor completes it once (a second attempt returns 409); the record is saved in the database; only its patient can read it (the other patient gets 403); the pending list no longer shows it.

Sanity check: changing one expected value (12 slots to 13) made the test fail; it was then restored.

## Web tests (11 cases)
Slots page: lists slots, empty state with the doctor name, error state, booking disabled until a patient ID is entered, booking success reloads the slots and trims the ID, booking conflict shows the error. Consultations page: empty message, load error, successful save (payload, list update, confirmation), empty prescription sent as null with a server error shown, and the 4000 and 2000 character limits on the text boxes.

## Flutter tests (16 cases)
`AppointmentSlot.fromJson` and `Doctor.fromJson`: valid data, a time with a UTC offset, each missing field, an invalid date, a wrong type, null values, list order and an empty list.

## Findings
- No product defect was found by these tests.
- Business rules confirmed: a Doctor may book on behalf of a patient (the booking endpoint allows staff), a consultation cannot be completed before the appointment starts, and slot generation is limited to 90 days.

## Limitations
- The in-memory database does not maintain the PostgreSQL concurrency token (`xmin`), so simultaneous double-booking is not tested here. It is covered on a real PostgreSQL server by `AppointmentBookingTests.TwoSimultaneousBookingAttempts_OnlyOneSucceeds` (not run in this session).
- The AI client is not exercised: Agent 3 is tested only for its failure path with no AI key.
- The web and Flutter tests use mocked data and do not test the live API, real GPS or real screens on a device.
- No performance or security scan was run specifically for these endpoints.
