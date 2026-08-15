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

export async function commitImportBatch(batchId, user = null) {
  return postJson(`/api/testcaseviewer/import/${encodeURIComponent(batchId)}/commit`, {}, 'Failed to commit import', user);
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
