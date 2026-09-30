# Functional integration handover — 28 September 2026

> Update, 29 September: Neon has now been upgraded and verified, with original records preserved in live tables or archive. Backend tests now total 109 after adding existing-schema compatibility coverage. See [NEON_UPGRADE_STATUS.md](NEON_UPGRADE_STATUS.md). The original local-verification report below records the earlier audit state.

Working branch: `feature/student4-dispatch`, with develop merged before repairs. Focus: all four students' existing features working together. The implementation is locally verified; no deployment or Neon migration was performed. The user requested local verification and prioritization of functional work.

## Implemented repairs

| Module | Working behavior and corrections |
| --- | --- |
| Student 1 | Patient-only public registration; authenticated and ownership-checked APIs; admin staff registration with atomic account/profile creation; private validated document uploads/downloads; shared client sessions; durable planner/workflow history. |
| Student 2 | Actual patient IDs and optional GPS destination; validated symptom input; linked four-step workflow; explicit safe failure/manual review; doctor-only decisions; patient case history and structured polling through visit completion; working doctor-search buttons. |
| Student 3 | Authorized roster/booking/consultation operations; real booked-appointment selector; actual patient identity in both normal and AI-search mobile booking; future-only search; clinic-date/time filters before limits; booking concurrency protection. |
| Student 4 | Approval prerequisite and reviewer identity; persistent waiting queue when no nurse is available; atomic nurse claiming and duplicate-request handling; assigned-nurse access; actual telemetry/destination; arrival before completion; escalation persisted; one vitals record and nurse release; visible expired-session failures and per-user/case attempted-vitals drafts. |
| Integration | Gemini configuration unified; bounded provider calls and deterministic tool/approval constraints; workflow events linked to business transitions; recovery of stale Running workflows into manual review; repaired migration replay/snapshot; portable API URLs/config templates and CI. |

## Verification actually completed

- **Backend: 108 passing tests** (106 API/unit/PostgreSQL tests plus 2 safety-rule golden tests), zero failures/skips. Includes real PostgreSQL migration replay and model/snapshot consistency, complete four-step workflow with deterministic provider fakes, authorization, concurrent nurse assignment, idempotent assignment/completion, interrupted-workflow recovery, rejected unapproved tool output, expired JWT, cross-patient access, 429/5xx retry limits and actual SQL specialty/date/time filtering.
- **React: 29 passing tests across 9 suites**; production build passed. New dispatch-store tests check pagination, authoritative availability, real coordinates and stale-data errors. Existing React Router future-flag warnings are non-failing.
- **Flutter: 39 passing tests**; `flutter analyze` reports no issues. New session/dispatch regressions check JWT reuse, expiry and propagation of failed location/completion responses.
- **Actual localhost API smoke:** health, Swagger, seeded admin login, patient registration and authenticated doctor listing passed against a separate migrated local database with synthetic credentials. Gemini disabled.
- **Measured local HTTP sample:** 100 authenticated doctor-list reads, concurrency 5, all 200, p50 2.01 ms, p95 3.48 ms, maximum 12.89 ms. Tiny synthetic dataset with no doctors; loopback macOS ARM64/PostgreSQL 18. This excludes AI/device/network load and is not a production capacity claim. Raw record: [LOCAL_PERFORMANCE_SAMPLE.json](LOCAL_PERFORMANCE_SAMPLE.json). Reproduce with `scripts/performance-smoke.py`.
- `git diff --check` passed. Private configuration remains ignored. Uploaded files were removed from Git tracking while preserved on this laptop.

## Start manual testing now

1. Follow [README](../README.md) to configure each laptop. Use the new migration on a new local database. For an existing/shared database, follow [DATABASE_UPGRADE](DATABASE_UPGRADE.md); preserve existing records and review any preflight issues.
2. Create two synthetic patients, doctors and nurses via the normal interfaces. Use each actual profile ID.
3. Execute [MANUAL_TEST_PLAN](MANUAL_TEST_PLAN.md), especially the whole-team chain: mobile patient → four agent steps → web doctor approval → waiting/assignment → mobile nurse GPS/arrival/vitals → patient completion.
4. Record observed results rather than marking the manual checklist passed based on automated tests. These changes are local working-tree edits, not a pushed release; teammates receive them only after commit/push and the team's normal integration process.

## Remaining limits and submission evidence

- No live Gemini quality evaluation, physical-device acceptance, release APK, deployed URLs, GitHub Actions run or Neon upgrade was completed. These are distinct from the passing local tests. Deployment was explicitly deferred by the user.
- Four executing workflow responsibilities are present; scheduling in that workflow uses a deterministic typed tool. Natural-language scheduling separately uses Gemini function calling. This is not automatic doctor booking, a universal privacy agent, real road routing or delivered SMS. Regex minimization cannot guarantee anonymity. ETA is a straight-line estimate and SMS remains simulated.
- Location tracking is foreground only. Failed attempted vitals are stored for 24 hours per account/case; this is not a general offline synchronization engine. Recovery routes interrupted work to staff rather than replaying side effects. A crash before triage linkage can leave only a saved workflow for inspection.
- The PDF also requires team/individual contribution evidence, deployment artifacts, ADRs, full AI evaluation and performance evidence, consolidated report, video, reflections and declarations. The [initial rubric audit](INITIAL_AUDIT.md) lists those requirements. Their completion cannot be inferred from code; the students must supply truthful evidence and their own reflections. No 100% compliance or clinical-production-readiness claim is made.
- Rotate previously committed credentials; removing source text does not revoke them. Detailed preservation/setup notes are in the migration guide.

Provider/schema reference checks: [Gemini structured output](https://ai.google.dev/gemini-api/docs/structured-output), [Gemini safety guidance](https://ai.google.dev/gemini-api/docs/safety-guidance), [EF migration deployment](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).
