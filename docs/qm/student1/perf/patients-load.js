// Student 1 (QM) – read-only load test of the patient registry API.
// Run: k6 run -e QM_EMAIL=... -e QM_PASSWORD=... [-e BASE_URL=http://localhost:5014] patients-load.js
// Credentials come from the environment only; never hard-code them in this file.
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '20s', target: 5 },   // ramp up
    { duration: '40s', target: 10 },  // steady load
    { duration: '10s', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],                                   // under 1% errors
    'http_req_duration{endpoint:list}': ['p(95)<1500'],              // list under 1.5 s (cloud DB)
    'http_req_duration{endpoint:search}': ['p(95)<1500'],
    checks: ['rate>0.99'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:5014';

export function setup() {
  const res = http.post(
    `${BASE}/api/v1/auth/login`,
    JSON.stringify({ email: __ENV.QM_EMAIL, password: __ENV.QM_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  if (!check(res, { 'login ok': (r) => r.status === 200 })) {
    throw new Error(`Login failed with status ${res.status}; stopping to avoid locking the account.`);
  }
  return { token: res.json('token') };
}

export default function (data) {
  const auth = { headers: { Authorization: `Bearer ${data.token}` } };

  const list = http.get(`${BASE}/api/v1/patients?page=1&pageSize=10`, { ...auth, tags: { endpoint: 'list' } });
  check(list, { 'list is 200': (r) => r.status === 200 });

  const search = http.get(`${BASE}/api/v1/patients?search=a&sortBy=fullName`, { ...auth, tags: { endpoint: 'search' } });
  check(search, { 'search is 200': (r) => r.status === 200 });

  // Negative control: an unauthenticated call must be rejected quickly under load.
  const anon = http.get(`${BASE}/api/v1/patients`, {
    tags: { endpoint: 'anon' },
    responseCallback: http.expectedStatuses(401), // 401 is the correct answer here, not a failure
  });
  check(anon, { 'anonymous is 401': (r) => r.status === 401 });

  sleep(1);
}
