# Student 4 report addendum - 8 October 2026

Use this addendum with the earlier Student 4 section. The earlier PDF/ZIP describes the 6 October runs and does not include this new defect.

## Section 3.4 - add three cases

| ID | Input / action | Expected | Actual |
| --- | --- | --- | --- |
| S4-TR-01 | Current location request fails; emit another GPS event | Error reported once; tracking stops; no second request | Passed after fix |
| S4-TR-02 | Pause/restart tracking while old request is pending; old request fails; emit new location | Old error ignored; new session sends location and receives success callback | Failed before fix; passed after fix |
| S4-TR-03 | Pause while request is pending; old request succeeds | No location callback after pause | Failed before fix; passed after fix |

Tool: flutter_test with controlled asynchronous API and position stream substitutes. No device GPS or live backend used. Source: mobile-app/test/services/dispatch_tracking_test.dart.

## Section 4.4 - replace the current no-defect summary

No product defect was found in the initial 6 October runs. An additional AI-assisted review on 8 October identified S4-DEF-01: a delayed location response from an old tracking session could affect a restarted session or publish an update after pause. Two regression tests failed before the fix. The cause was that cancelling the GPS stream did not invalidate callbacks from a pending HTTP request. Session generation checks and a session-local sending flag were added. The retest passed seven selected Flutter tests, and the student personally reran them with the same passing result. Suggested severity: Medium. Fixed locally; not yet committed/deployed as part of this addendum.

## Section 5 - update counts without double counting

Latest selected personal evidence covers 17 distinct cases across separate dates: 7 Flutter (4 existing session + 3 new tracking), 2 backend and 8 safety. The backend/safety results remain from 6 October. This is not one new full-suite run. Before-fix failures are retained as historical defect evidence, not counted as unresolved failures. One product defect was reproduced and fixed with AI assistance.

## Section 6.4 - evidence to add

- docs/qm/evidence/personal/student4-tracking-before.log (Codex: 2 failed regression cases).
- docs/qm/evidence/personal/student4-tracking-after.log (Codex: 7 passed).
- docs/qm/evidence/personal/student4-tracking-personal.log (student: 7 passed).
- docs/qm/evidence/personal/student4-tracking-review-20261008/ (source before fix, patch and hash manifest).
- docs/qm/student4-handover/DISPATCH_TRACKING_DEFECT.md (full defect record).

## Sections 7.4 and 8 - attribution

Codex investigated, reproduced and fixed the race, and wrote the regression tests. I personally ran the selected seven Flutter tests after the fix and saved the passing output. This is personal verification of AI-assisted work; I do not claim independent discovery or authorship of these new tests. Understanding/viva preparation is recorded separately. Add the new commit/PR link after publication; PR #11 covers only the earlier contribution.
