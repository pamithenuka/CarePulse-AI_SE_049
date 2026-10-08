SCHEDULING TESTS FOR THE GROUP - how to use them

These test files were prepared by a teammate (Student 1) for the group, with AI assistance (Claude Code).
They cover the scheduling area: roster, slots, booking, consultations and the Agent 3 search endpoint.
You (the owner of that area) should use them like this:

1. Copy the folders in this pack into your own checkout of the repo (same paths):
     tests/CarePulse.Api.Tests/Group/            (3 files)
     web-admin/src/pages/SchedulingPages.group.test.js
     mobile-app/test/models/scheduling_models_group_test.dart
     docs/qm/group/GROUP_SCHEDULING_TESTS.md

2. Read each test. You must be able to explain it in the viva.

3. Run them on YOUR machine (do not copy anyone's results):
     dotnet test tests/CarePulse.Api.Tests --filter "FullyQualifiedName~Group" --logger trx --results-directory docs/qm/evidence/personal/student3-run1
     cd web-admin ; $env:CI="true" ; npx.cmd react-scripts test --watchAll=false --testPathPattern "SchedulingPages.group" --verbose
     cd mobile-app ; flutter test test/models/scheduling_models_group_test.dart --reporter expanded
   Expected: 54 backend, 11 web, 16 Flutter, all passing. Save your own screenshots and logs.

4. Also run your existing 13 PostgreSQL tests (AppointmentBooking, Consultation, Roster, SlotGeneration, SchedulingSearch)
   and save the results.

5. Write at least 3 tests yourself (ideas: a Flutter booking-screen widget test, a k6 run on GET /api/v1/doctors/slots,
   a test for the Agent 3 tool-selection with a fake AI client) and run them.

6. Commit on YOUR branch with YOUR own name and student ID. In your contribution record and AI declaration, write
   honestly which tests were provided by a teammate and which you wrote, and that AI assistance was used.

7. Fill your sections of the group report (test cases, defects, evidence, contribution record, AI declaration).
