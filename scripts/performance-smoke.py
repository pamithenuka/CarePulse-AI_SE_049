"""Read-only HTTP latency sample. Use only against an authorized test deployment.
Set CAREPULSE_PERF_TOKEN locally for authenticated endpoints; tokens are never printed.
Example: python3 scripts/performance-smoke.py http://localhost:5014/health --requests 100 --concurrency 5
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
import math
import os
import platform
import time
import urllib.request
import urllib.error
from datetime import datetime, timezone

parser = argparse.ArgumentParser()
parser.add_argument("url")
parser.add_argument("--requests", type=int, default=100)
parser.add_argument("--concurrency", type=int, default=5)
args = parser.parse_args()
if not (1 <= args.requests <= 1000 and 1 <= args.concurrency <= 20):
    parser.error("Use 1–1000 requests and 1–20 concurrent clients.")
headers = {}
if os.environ.get("CAREPULSE_PERF_TOKEN"):
    headers["Authorization"] = "Bearer " + os.environ["CAREPULSE_PERF_TOKEN"]

def sample(_):
    started = time.perf_counter()
    try:
        request = urllib.request.Request(args.url, headers=headers)
        with urllib.request.urlopen(request, timeout=30) as response:
            response.read()
            status = response.status
    except urllib.error.HTTPError as error:
        status = error.code
    except (urllib.error.URLError, TimeoutError):
        status = 0
    return (time.perf_counter() - started) * 1000, status

with ThreadPoolExecutor(max_workers=args.concurrency) as executor:
    results = list(executor.map(sample, range(args.requests)))
times = sorted(duration for duration, _ in results)
statuses = {}
for _, status in results:
    statuses[str(status)] = statuses.get(str(status), 0) + 1
print(json.dumps({
    "measuredAtUtc": datetime.now(timezone.utc).isoformat(),
    "platform": platform.platform(), "url": args.url,
    "requests": args.requests, "concurrency": args.concurrency,
    "p50Ms": round(times[math.ceil(len(times) * .5) - 1], 2),
    "p95Ms": round(times[math.ceil(len(times) * .95) - 1], 2),
    "maxMs": round(max(times), 2), "statuses": statuses,
    "errorRate": sum(not 200 <= status < 300 for _, status in results) / len(results),
    "scope": "HTTP read sample only; excludes full AI workflow and device latency"
}, indent=2))
