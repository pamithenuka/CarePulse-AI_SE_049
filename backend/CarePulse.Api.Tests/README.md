# CarePulse – Backend Tests

Automated tests for Student 3's slice: roster validation, slot generation
(no duplicates), appointment booking, and - the one specifically required
by the assignment brief - **concurrency protection on double-booking**.

These tests run against a **separate test database** (`carepulse_test`),
never your real `carepulse` one, so running tests is always safe.

## One-time setup

1. **Copy this `CarePulse.Api.Tests` folder** into `backend/`, alongside `CarePulse.Api`, so you have:
   ```
   backend/
     CarePulse.Api/
     CarePulse.Api.Tests/
   ```

2. **Create the test database.** In a terminal:
   ```
   psql -U postgres
   ```
   Then:
   ```sql
   CREATE DATABASE carepulse_test;
   \q
   ```

3. **If your Postgres password isn't `postgres`**, open `CarePulse.Api.Tests/TestDatabaseFixture.cs` and update the `ConnectionString` at the top (same as you did for `appsettings.json` earlier).

## Running the tests

From inside `backend/CarePulse.Api.Tests/`:
```
dotnet restore
dotnet test
```

You should see output ending with something like:
```
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10
```

Each test run automatically wipes and rebuilds the test database schema fresh, so you can run `dotnet test` as many times as you like.

## What each file covers

- **`AppointmentBookingTests.cs`** — booking an open slot works; booking an already-booked slot is rejected; and the key one: **two simultaneous booking attempts on the same slot - only one may ever succeed.**
- **`ConsultationSummaryTests.cs`** — logging a visit summary works; can't log two summaries for the same visit; can't log a summary for a slot that was never booked.
- **`RosterTests.cs`** — valid roster updates work; unknown doctor is rejected; a start time after the end time is rejected.
- **`SlotGenerationTests.cs`** — generating slots from a roster produces the right count; running generation twice never creates duplicate slots.

## For your submission / evidence

Take a screenshot of the `dotnet test` output showing all tests passing -
that, plus this test code itself, is your "Individual Testing & Evidence"
for the concurrency requirement specifically called out in your brief.
