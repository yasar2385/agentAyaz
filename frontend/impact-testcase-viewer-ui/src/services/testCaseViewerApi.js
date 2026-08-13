const API_BASE = import.meta.env.VITE_TESTCASE_VIEWER_API_BASE ?? '';

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

export async function getFile(fileId) {
  const res = await fetch(`${API_BASE}/api/testcaseviewer/files/${encodeURIComponent(fileId)}`);
  if (!res.ok) {
    if (res.status === 404) return null;
    const text = await res.text();
    throw new Error(text || 'Failed to fetch file');
  }
  return res.json();
}
