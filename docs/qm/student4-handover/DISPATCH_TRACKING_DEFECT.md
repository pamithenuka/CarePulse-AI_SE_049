# S4-DEF-01 - Old location request affects restarted tracking

Date: 8 October 2026. Status: reproduced and fixed locally; not committed or deployed.
Suggested severity: Medium. Area: Flutter nurse dispatch tracking.

## Problem and reproduction

1. Start nurse tracking and send a location update whose response is delayed.
2. Pause tracking, then start tracking again before that response arrives.
3. Let the old request fail.

Expected: the old failure must not cancel or report an error against the new session.
Actual before fix: the old request called stopTracking and the error callback, affecting the restarted session. A second regression case showed that an old successful request still invoked the location callback after pause.

## Cause and fix

Cancelling the GPS subscription does not cancel a pending HTTP Future. Its success/error callbacks continued running and shared service-wide state with the next session.

LocationService now increments a session generation when stopping tracking. Callbacks only act if they belong to the current generation. The in-flight flag is scoped to each session. Optional API/stream injection permits deterministic testing; production defaults still use the existing API and Geolocator stream.

The fix does not retract an HTTP request already sent or undo a server update. It prevents obsolete callbacks from altering current client tracking.

## Evidence

- Before: `docs/qm/evidence/personal/student4-tracking-before.log`: 2 regression tests failed against the original logic with only dependency injection added.
- Before source: `docs/qm/evidence/personal/student4-tracking-review-20261008/location_service.before.dart.txt`.
- After: `docs/qm/evidence/personal/student4-tracking-after.log`: 7 passed (3 tracking tests plus 4 existing dispatch session tests).
- Test source: `mobile-app/test/services/dispatch_tracking_test.dart`.
- Production fix: `mobile-app/lib/features/dispatch/services/location_service.dart`.
- The third tracking test confirms that current-session errors still stop tracking and report exactly one error.
- An initial sandbox SDK-cache permission error was a tool setup issue, not the product failure; execution was retried with permission before collecting regression failures.

## Attribution and scope

Codex inspected the code, wrote and executed the tests, reproduced the defect, and implemented and verified the fix at the student's request. Student personally reran both Flutter files and supplied terminal output showing 7 passed on 8 October 2026. This is student verification of an AI-assisted fix, not independent student discovery or authorship. Understanding/review for the viva remains unverified.

Tests use controlled asynchronous HTTP and GPS substitutes. They do not verify physical GPS, Render, Neon, a real network outage or the latest remote main. Base checkout: df0bd5d6ccbf18c00bd66eb2039163e6d6884606 with local changes. No other dispatch defect is claimed as confirmed by this focused check.

## Student rerun

From mobile-app:

```sh
flutter test test/services/dispatch_tracking_test.dart test/services/dispatch_session_test.dart --reporter expanded
```

Actual personal rerun: 7 passed, saved in `docs/qm/evidence/personal/student4-tracking-personal.log` and inspected by Codex. The original 14-case evidence remains historical; the new 7-case run overlaps its 4 Flutter tests and must not be added as 7 new distinct cases. There are 3 new tracking cases, with separate execution attribution.

## Text for report section 4.4

During an additional AI-assisted review on 8 October, a tracking race was reproduced: a delayed location response from a paused session could affect a restarted session. Two regression tests failed before the fix. Session generation checks were added so obsolete callbacks cannot stop or update current tracking. The retest passed seven selected Flutter tests, including three tracking cases and four existing session cases. Codex performed the investigation, implementation and initial before/after execution. The student subsequently ran both test files and supplied terminal output confirming all seven passed. This is a local client reliability fix, not a deployed GPS test.
