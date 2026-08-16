const API_BASE = import.meta.env.VITE_TESTCASE_VIEWER_API_BASE ?? '';

export async function login(username, password) {
  const res = await fetch(`${API_BASE}/api/testcaseviewer/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(readErrorMessage(text) || 'Login failed');
  }

  return res.json();
}

export async function getFiles(reportType = 'master') {
  const res = await fetch(`${API_BASE}/api/testcaseviewer/files?reportType=${encodeURIComponent(reportType)}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || 'Failed to fetch files');
  }
  return res.json();
}

export async function getKnownFile(name) {
  return getJson(`/api/testcaseviewer/files/known/${encodeURIComponent(name)}`, 'Failed to fetch known file');
}

export async function getSheets(fileId) {
  return getJson(`/api/testcaseviewer/files/${encodeURIComponent(fileId)}/sheets`, 'Failed to fetch sheets');
}

export async function getDashboardSummary(fileId) {
  return getJson(`/api/testcaseviewer/files/${encodeURIComponent(fileId)}/dashboard-summary`, 'Failed to fetch dashboard summary');
}

export async function getDashboardCache(reportType = 'master', user = null) {
  return getJson(
    `/api/testcaseviewer/dashboard-cache?reportType=${encodeURIComponent(reportType)}`,
    'Failed to fetch dashboard cache',
    user,
  );
}

export async function getOfflineDashboardCache(reportType = 'master', user = null) {
  return getJson(
    `/api/testcaseviewer/dashboard-cache/offline?reportType=${encodeURIComponent(reportType)}`,
    'Failed to fetch offline dashboard cache',
    user,
  );
}

export async function refreshDashboardFile(payload, user = null) {
  return postJson('/api/testcaseviewer/dashboard-cache/refresh-file', withUser(payload, user), 'Failed to refresh dashboard file', user);
}

export async function refreshDashboardSheet(payload, user = null) {
  return postJson('/api/testcaseviewer/dashboard-cache/refresh-sheet', withUser(payload, user), 'Failed to refresh dashboard sheet', user);
}

export async function refreshRegressionIndex(user = null) {
  return postJson('/api/testcaseviewer/dashboard-cache/refresh-regression-index', {}, 'Failed to refresh regression index', user);
}

export async function syncChangedFiles(reportType = 'master', user = null) {
  return postJson(
    `/api/testcaseviewer/dashboard-cache/sync-changed-files?reportType=${encodeURIComponent(reportType)}`,
    {},
    'Failed to sync changed files',
    user,
  );
}

export async function exportTsv(reportType = 'master', user = null) {
  return postJson(
    `/api/testcaseviewer/dashboard-cache/export-tsv?reportType=${encodeURIComponent(reportType)}`,
    {},
    'Failed to export TSV',
    user,
  );
}

export async function saveDashboardChanges(payload, user = null) {
  return postJson('/api/testcaseviewer/dashboard-cache/save-changes', withUser(payload, user), 'Failed to save dashboard changes', user);
}

export async function loadSourceUrl(url, reportType = 'master', user = null) {
  return postJson(
    '/api/testcaseviewer/dashboard-cache/load-url',
    withUser({ url, reportType }, user),
    'Failed to load source URL',
    user,
  );
}

export async function downloadToLocal(source, reportType = 'master', user = null) {
  return postJson(
    '/api/testcaseviewer/dashboard-cache/download-local',
    withUser({ source, reportType, downloadScope: 'allSheets' }, user),
    'Failed to download source to local',
    user,
  );
}

export async function uploadMasterImport(file, user = null) {
  const formData = new FormData();
  formData.append('file', file);
  return postForm('/api/testcaseviewer/import/master/upload', formData, 'Failed to upload master test cases', user);
}

export async function inspectImportFile(file, user = null) {
  const formData = new FormData();
  formData.append('file', file);
  return postForm('/api/testcaseviewer/import/inspect', formData, 'Failed to inspect import file', user);
}

export async function parseMasterImport(uploadToken, sheetNames, user = null) {
  return postJson('/api/testcaseviewer/import/master/parse', { uploadToken, sheetNames }, 'Failed to parse master test cases', user);
}

export async function uploadResultImport(files, resultMode = 'single', user = null) {
  const formData = new FormData();
  files.forEach(file => formData.append('files', file));
  formData.append('resultMode', resultMode);
  return postForm('/api/testcaseviewer/import/results/upload', formData, 'Failed to upload test results', user);
}

export async function getImportBatch(batchId, user = null) {
  return getJson(`/api/testcaseviewer/import/${encodeURIComponent(batchId)}`, 'Failed to fetch import batch', user);
}

export async function getImportErrors(batchId, user = null) {
  return getJson(`/api/testcaseviewer/import/${encodeURIComponent(batchId)}/errors`, 'Failed to fetch import errors', user);
}

export async function saveMasterSheetActions(batchId, actions, user = null) {
  return postJson(
    `/api/testcaseviewer/import/master/${encodeURIComponent(batchId)}/sheet-actions`,
    { actions },
    'Failed to save sheet actions',
    user,
  );
}

export async function saveManualEditImportActions(batchId, actions, user = null) {
  return postJson(
    `/api/testcaseviewer/import/master/${encodeURIComponent(batchId)}/manual-edit-actions`,
    { actions },
    'Failed to save manual edit conflict actions',
    user,
  );
}

export async function commitImportBatch(batchId, user = null) {
  return postJson(`/api/testcaseviewer/import/${encodeURIComponent(batchId)}/commit`, {}, 'Failed to commit import', user);
}

export async function getMasterReviewModules(user = null) {
  return getJson('/api/testcaseviewer/master/modules', 'Failed to fetch master modules', user);
}

export async function getMasterReviewList(moduleId, page = 1, pageSize = 25, user = null) {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (moduleId) query.set('moduleId', String(moduleId));
  return getJson(`/api/testcaseviewer/master?${query.toString()}`, 'Failed to fetch master test cases', user);
}

export async function getMasterReviewLookups(user = null) {
  return getJson('/api/testcaseviewer/master/lookups', 'Failed to fetch master lookups', user);
}

export async function getMasterReviewDetail(masterTestId, user = null) {
  return getJson(`/api/testcaseviewer/master/${encodeURIComponent(masterTestId)}`, 'Failed to fetch master test case', user);
}

export async function createMasterReviewDetail(payload, user = null) {
  return postJson('/api/testcaseviewer/master', payload, 'Failed to create master test case', user);
}

export async function updateMasterReviewDetail(masterTestId, payload, user = null) {
  return putJson(`/api/testcaseviewer/master/${encodeURIComponent(masterTestId)}`, payload, 'Failed to save master test case', user);
}

export async function deleteMasterReviewDetail(masterTestId, user = null) {
  return deleteJson(`/api/testcaseviewer/master/${encodeURIComponent(masterTestId)}`, 'Failed to delete master test case', user);
}

export async function getPlaywrightReadiness(user = null) {
  return getJson('/api/testcaseviewer/runs/readiness', 'Failed to fetch Playwright readiness', user);
}

export async function getRunMetadata(user = null) {
  return getJson('/api/testcaseviewer/runs/metadata', 'Failed to fetch run metadata', user);
}

export async function getRunConfigs(user = null) {
  return getJson('/api/testcaseviewer/runs/configs', 'Failed to fetch run configs', user);
}

export async function saveRunConfig(payload, user = null, configId = null) {
  if (configId) {
    return putJson(`/api/testcaseviewer/runs/configs/${encodeURIComponent(configId)}`, payload, 'Failed to update run config', user);
  }
  return postJson('/api/testcaseviewer/runs/configs', payload, 'Failed to save run config', user);
}

export async function triggerRunConfig(configId, user = null) {
  return postJson(`/api/testcaseviewer/runs/configs/${encodeURIComponent(configId)}/trigger`, {}, 'Failed to trigger run', user);
}

export async function getRunExecution(executionId, user = null) {
  return getJson(`/api/testcaseviewer/runs/executions/${encodeURIComponent(executionId)}`, 'Failed to fetch run execution', user);
}

export async function cancelRunExecution(executionId, user = null) {
  return postJson(`/api/testcaseviewer/runs/executions/${encodeURIComponent(executionId)}/cancel`, {}, 'Failed to cancel run', user);
}

export async function getRecentRuns(scope = 'mine', limit = 20, user = null) {
  return getJson(
    `/api/testcaseviewer/runs/recent?scope=${encodeURIComponent(scope)}&limit=${encodeURIComponent(limit)}`,
    'Failed to fetch recent runs',
    user,
  );
}

export async function getRunProgress(configId, user = null) {
  return getJson(`/api/testcaseviewer/runs/${encodeURIComponent(configId)}/progress`, 'Failed to fetch run progress', user);
}

export async function continueRunConfig(configId, user = null) {
  return postJson(`/api/testcaseviewer/runs/${encodeURIComponent(configId)}/continue`, {}, 'Failed to continue testing', user);
}

export async function verifyFix(payload, user = null) {
  return postJson('/api/testcaseviewer/runs/verify-fix', payload, 'Failed to verify bug fix', user);
}

export function runReportUrl(executionId) {
  return `${API_BASE}/api/testcaseviewer/runs/executions/${encodeURIComponent(executionId)}/report`;
}

export async function getSheetRows(fileId, sheetName) {
  return getJson(
    `/api/testcaseviewer/files/${encodeURIComponent(fileId)}/sheets/${encodeURIComponent(sheetName)}/rows`,
    'Failed to fetch sheet rows',
  );
}

async function getJson(path, fallbackMessage, user = null) {
  const res = await fetch(`${API_BASE}${path}`, { headers: authHeaders(user) });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || fallbackMessage);
  }
  return res.json();
}

async function postJson(path, payload, fallbackMessage, user = null) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders(user) },
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(readErrorMessage(text) || fallbackMessage);
  }
  return res.json();
}

async function putJson(path, payload, fallbackMessage, user = null) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders(user) },
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(readErrorMessage(text) || fallbackMessage);
  }
  return res.json();
}

async function deleteJson(path, fallbackMessage, user = null) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'DELETE',
    headers: authHeaders(user),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(readErrorMessage(text) || fallbackMessage);
  }
  return res.json();
}

async function postForm(path, formData, fallbackMessage, user = null) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: authHeaders(user),
    body: formData,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(readErrorMessage(text) || fallbackMessage);
  }
  return res.json();
}

function withUser(payload, user) {
  return user ? { ...payload, user } : payload;
}

function authHeaders(user) {
  if (!user) return {};
  return {
    'X-TestCaseViewer-UserId': user.id ?? '',
    'X-TestCaseViewer-Username': user.username ?? '',
    'X-TestCaseViewer-DisplayName': user.displayName ?? '',
    'X-TestCaseViewer-Role': typeof user.role === 'string' ? user.role : JSON.stringify(user.role ?? ''),
    'X-TestCaseViewer-Email': user.email ?? '',
  };
}

function readErrorMessage(text) {
  if (!text) return '';
  try {
    const parsed = JSON.parse(text);
    return parsed.message || parsed.title || text;
  } catch {
    return text;
  }
}

export async function getFile(fileId) {
  const res = await fetch(`${API_BASE}/api/testcaseviewer/files/${encodeURIComponent(fileId)}`);
  if (!res.ok) {
    if (res.status === 404) return null;
    const text = await res.text();
    throw new Error(text || 'Failed to fetch file');
  }
  return res.json();
}
