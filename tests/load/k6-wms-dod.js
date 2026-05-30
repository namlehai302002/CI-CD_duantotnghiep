import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.WMS_BASE_URL;
const authStatePath = __ENV.WMS_AUTH_STATE || 'tests/visual/.auth/wms-auth-state.json';
const authCookie = __ENV.WMS_AUTH_COOKIE || cookieFromAuthState(authStatePath, baseUrl);
const apiKey = __ENV.WMS_API_KEY || apiKeyFromAppsettings();
const loadProfile = (__ENV.WMS_LOAD_PROFILE || '100').toString();
const mutationEnabled = (__ENV.WMS_K6_MUTATION_ENABLED || '').toLowerCase() === 'true';
const seededPickTaskId = __ENV.WMS_K6_PICK_TASK_ID || '';
const seededPickQty = __ENV.WMS_K6_PICK_QTY || '0';
const summaryPath = __ENV.WMS_K6_SUMMARY_PATH || `artifacts/load/k6-summary-${loadProfile}.json`;

if (!baseUrl) {
  throw new Error('WMS_BASE_URL is required for load tests.');
}

if (mutationEnabled && !seededPickTaskId) {
  throw new Error('WMS_K6_PICK_TASK_ID is required when WMS_K6_MUTATION_ENABLED=true.');
}

export const options = {
  tags: {
    profile: loadProfile
  },
  scenarios: {
    inventory_posting_reads: {
      executor: 'ramping-vus',
      exec: 'inventoryPostingReads',
      stages: [
        { duration: '1m', target: 10 },
        { duration: '3m', target: 25 },
        { duration: '1m', target: 0 }
      ]
    },
    scan_queue_retry: {
      executor: 'constant-vus',
      exec: 'scanQueueRetry',
      vus: 10,
      duration: '3m'
    },
    large_reports: {
      executor: 'constant-arrival-rate',
      exec: 'largeReports',
      rate: 12,
      timeUnit: '1m',
      duration: '5m',
      preAllocatedVUs: 8
    },
    three_pl_billing: {
      executor: 'constant-vus',
      exec: 'threePlBilling',
      vus: 8,
      duration: '4m'
    },
    integration_api: {
      executor: 'constant-arrival-rate',
      exec: 'integrationApi',
      rate: 20,
      timeUnit: '1m',
      duration: '5m',
      preAllocatedVUs: 10
    },
    bi_sre_dashboards: {
      executor: 'constant-arrival-rate',
      exec: 'biSreDashboards',
      rate: profileRate(),
      timeUnit: '1m',
      duration: '5m',
      preAllocatedVUs: profileVus()
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1500']
  }
};

function profileRate() {
  if (loadProfile === '1000') return 100;
  if (loadProfile === '500') return 50;
  return 20;
}

function profileVus() {
  if (loadProfile === '1000') return 80;
  if (loadProfile === '500') return 40;
  return 12;
}

function readJsonFile(path) {
  try {
    return JSON.parse(open(path));
  } catch (_) {
    return null;
  }
}

function cookieFromAuthState(path, targetBaseUrl) {
  if (!targetBaseUrl) return '';
  const state = readJsonFile(path);
  if (!state || !Array.isArray(state.cookies)) return '';
  let host = '';
  try {
    host = new URL(targetBaseUrl).hostname;
  } catch (_) {
    return '';
  }

  return state.cookies
    .filter(cookie => cookie?.name && cookie?.value && (!cookie.domain || host.endsWith(cookie.domain.replace(/^\./, ''))))
    .map(cookie => `${cookie.name}=${cookie.value}`)
    .join('; ');
}

function apiKeyFromAppsettings() {
  const settings = readJsonFile('appsettings.json');
  return settings?.Api?.Key || '';
}

function headers(extra = {}) {
  return {
    headers: {
      Cookie: authCookie,
      'X-API-Key': apiKey,
      ...extra
    }
  };
}

function isLoginResponse(response) {
  return response.url.includes('/Account/Login')
    || (typeof response.body === 'string' && (
      response.body.includes('name="UserName"')
      || response.body.includes('name="Password"')
      || response.body.includes('type="submit"')
    ));
}

function assertOk(response, name) {
  check(response, {
    [`${name} status is successful`]: r => r.status >= 200 && r.status < 400,
    [`${name} does not fall back to login`]: r => !isLoginResponse(r),
    [`${name} responds`]: r => r.status > 0
  });
}

export function inventoryPostingReads() {
  assertOk(http.get(`${baseUrl}/Reports/InventoryTransactions`, headers()), 'inventory transactions');
  assertOk(http.get(`${baseUrl}/Reports/StockMovement`, headers()), 'stock movement');
  sleep(1);
}

export function scanQueueRetry() {
  if (!mutationEnabled) {
    assertOk(http.get(`${baseUrl}/Operations/RfPicking`, headers()), 'rf picking readiness');
    sleep(1);
    return;
  }

  const operationId = `k6-scan-${__VU}-${__ITER}`;
  const payload = {
    id: seededPickTaskId,
    qty: seededPickQty,
    scanValue: 'K6-SCAN',
    queuedBaselinePickedQty: '0'
  };
  assertOk(http.post(
    `${baseUrl}/Vouchers/ConfirmPickTask`,
    payload,
    headers({
      'Content-Type': 'application/x-www-form-urlencoded',
      'X-WMS-Queued-Operation': 'true',
      'X-WMS-Offline-Operation-Id': operationId
    })
  ), 'scan queue retry');
  sleep(1);
}

export function largeReports() {
  assertOk(http.get(`${baseUrl}/Reports/Inventory`, headers()), 'inventory report');
  assertOk(http.get(`${baseUrl}/Reports/StockValuation`, headers()), 'stock valuation');
  sleep(1);
}

export function threePlBilling() {
  assertOk(http.get(`${baseUrl}/Operations/ThreePlBillingRuns`, headers()), '3pl runs');
  assertOk(http.get(`${baseUrl}/Operations/ThreePlBillingRates`, headers()), '3pl rates');
  sleep(1);
}

export function integrationApi() {
  assertOk(http.get(`${baseUrl}/api/integration/items`, headers()), 'api items');
  assertOk(http.get(`${baseUrl}/api/integration/stock`, headers()), 'api stock');
  assertOk(http.get(`${baseUrl}/api/integration/vouchers`, headers()), 'api vouchers');
  assertOk(http.get(`${baseUrl}/api/integration/kpi`, headers()), 'api kpi');
  sleep(1);
}

export function biSreDashboards() {
  assertOk(http.get(`${baseUrl}/Reports/SemanticBi`, headers()), 'semantic bi');
  assertOk(http.get(`${baseUrl}/Reports/PredictiveAlerts`, headers()), 'predictive alerts');
  assertOk(http.get(`${baseUrl}/System/SreDashboard`, headers()), 'sre dashboard');
  sleep(1);
}

export function handleSummary(data) {
  return {
    [summaryPath]: JSON.stringify({
      generatedAt: new Date().toISOString(),
      baseUrl,
      loadProfile,
      mutationEnabled,
      thresholds: data.thresholds,
      metrics: data.metrics
    }, null, 2),
    stdout: `k6 summary written to ${summaryPath}\n`
  };
}
