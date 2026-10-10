# Performance evaluation protocol

Declared 3 October 2026 before load execution. Target: isolated CarePulse.ClientHarness, loopback port 5516, real PostgreSQL. No Gemini calls, Neon traffic or production user data.

## Dataset and workload

Enable `CAREPULSE_PERF_DATA=1`: fixture totals are 501 synthetic patients, 51 doctors, 1001 future appointment slots and one nurse. The benchmark verifies nonempty expected response counts before load. Authenticated doctor GET requests are split equally among doctor directory, cardiology slot search and a 20-item patient registry page.

Use `scripts/qm-performance.py`, a reproducible Python standard-library HTTP workload generator. This is a justified alternative to k6 for bounded local testing: explicit concurrency, request/error validation, latency percentiles, throughput and raw per-request samples, with no added installation. It is not a distributed load generator.

Twelve excluded warm-up requests; 600 measured requests per stage: concurrency 1 baseline, 5 target load, 20 exploratory stress, then 1 recovery sample. Two-second pauses between stages. Fixed-count, closed-loop workload with no user think time; stage durations are measured, not predetermined. This short test does not establish sustained capacity or a breaking point.

Predeclared acceptance (from TEST_PLAN.md): at five clients, p95 <1000 ms and unexpected error rate <1%. HTTP failures, parse failures and wrong dataset shapes/counts count as errors. Twenty clients are exploratory; do not change acceptance thresholds after observing results.

## Reproduce

Start isolated PostgreSQL. From project root, set local `CAREPULSE_TEST_CONNECTION`, a private `/tmp` handoff path as `CAREPULSE_CLIENT_SESSION`, and `CAREPULSE_PERF_DATA=1`, then run:

```sh
dotnet run --configuration Release --project tests/CarePulse.ClientHarness
```

Wait for port 5516, then in another terminal:

```sh
python3 scripts/qm-performance.py --session /tmp/carepulse-qm-perf-session.json --output NEW_PERFORMANCE_RESULTS.json
```

Use the same handoff path in both terminals. This script fixes its target to loopback 5516 and requires the harness performance-data flag. Preserve output; reruns require a new filename. Stop the harness normally with Ctrl+C so its disposable database and token file are removed.

## Interpretation boundaries

Kestrel forwards requests into WebApplicationFactory's in-process server; measurements include that forwarding and client JSON parsing. Backend and generator share a laptop, and database/cache state influences results. This is not deployed API capacity. No claim is made about AI latency, concurrent writes, long-running leaks, mobile networks or clinical response times. Directory/slot endpoints return the full matching dataset; larger datasets warrant separate pagination evaluation.

## Actual result — 3 October 2026

| Stage | Requests | Duration (s) | Requests/s | p95 (ms) | Errors |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1-client baseline | 600 | 1.68 | 357.13 | 4.591 | 0 |
| 5-client target | 600 | 0.591 | 1015.04 | 9.921 | 0 |
| 20-client exploration | 600 | 0.362 | 1657.77 | 30.684 | 0 |
| 1-client recovery | 600 | 0.848 | 707.17 | 2.052 | 0 |

**Declared five-client acceptance passed.** All 2,400 measured requests returned valid expected data with no errors. Twelve warm-up requests are excluded. The final one-client sample also passed, but this is not a long-duration recovery/soak test. Lower later latency is consistent with warming caches/runtime; this run does not isolate the cause. No saturation point was reached or established. Raw samples: `evidence/performance-20261003/results.json`; machine/source details: `metadata.json`.
