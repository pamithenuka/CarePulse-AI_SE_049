"""Bounded loopback workload for CarePulse.ClientHarness. No third-party packages required."""
import argparse, concurrent.futures, datetime, json, math, pathlib, platform, time, urllib.request

p = argparse.ArgumentParser()
p.add_argument('--session', required=True, help='Private ClientHarness handoff file')
p.add_argument('--output', required=True, help='New evidence JSON file')
a = p.parse_args()
out = pathlib.Path(a.output)
if out.exists(): p.error('Output already exists; preserve previous runs.')
session = json.loads(pathlib.Path(a.session).read_text())
if not session.get('performanceData'): p.error('Start harness with CAREPULSE_PERF_DATA=1.')
base = 'http://127.0.0.1:5516/api/v1'
# A fixed target prevents accidentally load-testing Neon-connected apps or external services.
paths = ['/doctors', '/doctors/slots?specialty=CARDIOLOGY', '/patients?page=1&pageSize=20']
headers = {'Authorization': 'Bearer ' + session['doctorToken']}

def sample(i):
    path = paths[i % len(paths)]
    started = time.perf_counter()
    status, valid, error = 0, False, None
    try:
        with urllib.request.urlopen(urllib.request.Request(base + path, headers=headers), timeout=15) as r:
            status = r.status
            body = json.load(r)
            if i % 3 == 0: valid = isinstance(body, list) and len(body) == 51
            elif i % 3 == 1: valid = isinstance(body, list) and len(body) == 1001
            else: valid = isinstance(body, dict) and len(body.get('items', [])) == 20
    except Exception as e:
        # Do not serialize requests, headers, tokens or provider response bodies.
        error = type(e).__name__
    return {'endpoint': path, 'ms': round((time.perf_counter()-started)*1000, 3),
            'status': status, 'valid': valid, 'errorType': error}

warmup = [sample(i) for i in range(12)]
if any(r['status'] != 200 or not r['valid'] for r in warmup):
    out.write_text(json.dumps({'warmup': warmup, 'status': 'Blocked: dataset/endpoint preflight failed'}, indent=2))
    raise SystemExit('Dataset/endpoint preflight failed; see output. No measured load started.')
runs=[]
for concurrency in [1, 5, 20, 1]:
    started=time.perf_counter()
    with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
        rows=list(pool.map(sample, range(600)))
    elapsed=time.perf_counter()-started
    times=sorted(r['ms'] for r in rows)
    errors=sum(r['status'] != 200 or not r['valid'] for r in rows)
    summary={'concurrency': concurrency, 'requests':len(rows),'durationSeconds':round(elapsed,3),
        'requestsPerSecond':round(len(rows)/elapsed,2),'p50Ms':times[math.ceil(len(times)*.50)-1],
        'p95Ms':times[math.ceil(len(times)*.95)-1],'p99Ms':times[math.ceil(len(times)*.99)-1],
        'maxMs':max(times),'errors':errors,'errorRate':errors/len(rows),'samples':rows}
    runs.append(summary)
    print(f"concurrency={concurrency}: p95={summary['p95Ms']}ms; errors={errors}/{len(rows)}")
    time.sleep(2)
result={'timestampUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'platform':platform.platform(),
    'tool':'Python standard library ThreadPoolExecutor + urllib, fixed request-count closed-loop workload',
    'dataset':{'patients':501,'doctors':51,'slots':1001,'nurses':1},
    'mix':'Equal GET doctor directory, cardiology slots, first patient-registry page',
    'warmupRequests':12,'threshold':{'concurrency':5,'p95MsLessThan':1000,'errorRateLessThan':.01},
    'runs':runs,'acceptancePassed':runs[1]['p95Ms']<1000 and runs[1]['errorRate']<.01,
    'limitations':'Short local read-only bursts through test-host forwarding; excludes live AI, browser/mobile, writes, WAN, soak and production capacity.'}
out.write_text(json.dumps(result,indent=2)+'\n')
raise SystemExit(0 if result['acceptancePassed'] else 1)
