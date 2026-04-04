/**
 * k6 load test — Bank Sync API
 * T518: Simulate 100 concurrent users, each connecting → syncing → querying dashboard.
 * SLA: p99 latency < 5 sec, error rate < 1%
 *
 * Run: k6 run bank-sync-load-test.js
 * Run with output: k6 run --out json=results.json bank-sync-load-test.js
 */
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const connectDuration = new Trend('connect_duration', true);
export const dashboardDuration = new Trend('dashboard_duration', true);

export const options = {
  vus: 100,
  duration: '2m',
  thresholds: {
    'http_req_duration': ['p(99)<5000'],
    'errors': ['rate<0.01'],
    'connect_duration': ['p(95)<2000'],
    'dashboard_duration': ['p(95)<1000'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const JWT_TOKEN = __ENV.JWT_TOKEN || 'test-token';
const USER_ID   = __ENV.USER_ID   || '00000000-0000-0000-0000-000000000001';

const headers = {
  'Authorization': `Bearer ${JWT_TOKEN}`,
  'Content-Type': 'application/json',
};

export default function () {
  // Step 1: Get Plaid link token
  const connectStart = Date.now();
  const connectRes = http.post(
    `${BASE_URL}/api/accounts/connect`,
    JSON.stringify({ userId: USER_ID }),
    { headers }
  );
  connectDuration.add(Date.now() - connectStart);

  const connectOk = check(connectRes, {
    'connect: status 200': (r) => r.status === 200,
    'connect: has linkToken': (r) => r.json('linkToken') !== undefined,
  });
  errorRate.add(!connectOk);

  if (!connectOk) { sleep(1); return; }

  // Step 2: List accounts (simulates returning user)
  const accountsRes = http.get(
    `${BASE_URL}/api/accounts?userId=${USER_ID}`,
    { headers }
  );

  const accountsOk = check(accountsRes, {
    'accounts: status 200': (r) => r.status === 200,
  });
  errorRate.add(!accountsOk);

  // Step 3: Query dashboard aggregated
  const dashStart = Date.now();
  const dashRes = http.get(
    `${BASE_URL}/api/dashboard/aggregated?userId=${USER_ID}`,
    { headers }
  );
  dashboardDuration.add(Date.now() - dashStart);

  const dashOk = check(dashRes, {
    'dashboard: status 200': (r) => r.status === 200,
    'dashboard: latency < 1s': (r) => r.timings.duration < 1000,
  });
  errorRate.add(!dashOk);

  sleep(1);
}
