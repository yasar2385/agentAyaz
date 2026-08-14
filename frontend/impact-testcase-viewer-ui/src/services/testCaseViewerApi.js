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

export async function getDashboardCache(reportType = 'master') {
  return getJson(
    `/api/testcaseviewer/dashboard-cache?reportType=${encodeURIComponent(reportType)}`,
    'Failed to fetch dashboard cache',
  );
}

export async function refreshDashboardFile(payload) {
  return postJson('/api/testcaseviewer/dashboard-cache/refresh-file', payload, 'Failed to refresh dashboard file');
}

export async function refreshDashboardSheet(payload) {
  return postJson('/api/testcaseviewer/dashboard-cache/refresh-sheet', payload, 'Failed to refresh dashboard sheet');
}

export async function refreshRegressionIndex() {
  return postJson('/api/testcaseviewer/dashboard-cache/refresh-regression-index', {}, 'Failed to refresh regression index');
}

export async function getSheetRows(fileId, sheetName) {
  return getJson(
    `/api/testcaseviewer/files/${encodeURIComponent(fileId)}/sheets/${encodeURIComponent(sheetName)}/rows`,
    'Failed to fetch sheet rows',
  );
}

async function getJson(path, fallbackMessage) {
  const res = await fetch(`${API_BASE}${path}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || fallbackMessage);
  }
  return res.json();
}

async function postJson(path, payload, fallbackMessage) {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(readErrorMessage(text) || fallbackMessage);
  }
  return res.json();
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
