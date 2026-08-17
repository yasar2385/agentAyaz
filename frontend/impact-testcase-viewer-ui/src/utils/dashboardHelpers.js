export function statusTone(status) {
  const value = String(status).toLowerCase()
  if (value.includes('pass') || value.includes('closed') || value.includes('fixed')) return 'good'
  if (value.includes('fail') || value.includes('reject') || value.includes('reopen')) return 'bad'
  if (value.includes('wip') || value.includes('clear')) return 'warn'
  return 'neutral'
}

export function toggleValue(values, value) {
  return values.includes(value) ? values.filter(item => item !== value) : [...values, value]
}

export function uniqueValues(rows, key) {
  return [...new Set(rows.map(row => row[key]).filter(value => String(value ?? '').trim()))].sort((a, b) =>
    String(a).localeCompare(String(b)),
  )
}

export function latestRound(row) {
  const rounds = row.rounds ?? []
  return rounds.length > 0 ? rounds[rounds.length - 1] : null
}

export function roundLabel(row) {
  const round = latestRound(row)
  return round ? `R${round.roundNumber}` : 'R1'
}

export function groupRows(rows, keySelector) {
  return rows.reduce((groups, row) => {
    const key = keySelector(row) || 'Unassigned'
    const existing = groups.get(key) ?? []
    existing.push(row)
    groups.set(key, existing)
    return groups
  }, new Map())
}

export function groupEntries(rows, keySelector) {
  return [...groupRows(rows, keySelector).entries()].sort(([a], [b]) => a.localeCompare(b))
}

export function statusIsProblem(status) {
  const value = String(status ?? '').toLowerCase()
  return ['fail', 'failed', 'reject', 'reopen', 'error', 'bug'].some(term => value.includes(term))
}

export function isRepeatedRow(row, duplicateKeys) {
  const identity = row.testCaseId || row.testCaseNo
  return (
    (identity && duplicateKeys.has(identity)) ||
    statusIsProblem(row.qaStatus) ||
    statusIsProblem(row.devStatus) ||
    statusIsProblem(row.issueType)
  )
}

export function isPostponedRow(row) {
  const value = [
    row.issueType,
    row.qaStatus,
    row.devStatus,
    row.actualResult,
    ...(row.qaRemarks ?? []),
    ...(row.devRemarks ?? []),
  ].join(' ').toLowerCase()

  return ['postpon', 'future development', 'future dev', 'defer', 'later', 'hold'].some(term => value.includes(term))
}

export function cacheFileToOption(file) {
  return {
    id: file.fileId,
    name: file.fileName,
    reportType: file.reportType,
    lastScannedAt: file.lastScannedAt,
    scanStatus: file.scanStatus,
    scanError: file.scanError,
    syncStatus: file.syncStatus,
    syncError: file.syncError,
    pendingEditCount: file.pendingEditCount,
    localTsvPath: file.localTsvPath,
    lastLocalSyncAt: file.lastLocalSyncAt,
    lastMetadataSyncedAt: file.lastMetadataSyncedAt,
    lastDriveCheckedAt: file.lastDriveCheckedAt,
    driveModifiedTime: file.driveModifiedTime,
    sourceUrl: file.sourceUrl,
    folderUrl: file.folderUrl,
    sheets: file.sheets ?? [],
  }
}

export function cacheSheetToInfo(sheet, index) {
  return {
    name: sheet.sheetName,
    index,
    rowCount: sheet.totalTestCases ?? 0,
    columnCount: 0,
  }
}

export function cacheSheetToRowsResponse(fileId, sheet) {
  return {
    fileId,
    sheetName: sheet?.sheetName ?? '',
    rows: sheet?.rows ?? [],
    qaStatuses: uniqueValues(sheet?.rows ?? [], 'qaStatus'),
    devStatuses: uniqueValues(sheet?.rows ?? [], 'devStatus'),
  }
}

export function cacheFileToSummary(file) {
  const sheets = file?.sheets ?? []
  const qaStatusCounts = {}
  const devStatusCounts = {}
  let totalTestCases = 0

  sheets.forEach(sheet => {
    totalTestCases += sheet.totalTestCases ?? 0
    addCount(qaStatusCounts, 'Pass', sheet.passCount)
    addCount(qaStatusCounts, 'Failed', sheet.failedCount)
    addCount(qaStatusCounts, 'Postponed', sheet.postponedCount)
    addCount(qaStatusCounts, 'WIP', sheet.wipCount)
    addCount(qaStatusCounts, 'Not clear', sheet.notClearCount)
    addCount(qaStatusCounts, 'Future Development', sheet.futureDevelopmentCount)
    addCount(devStatusCounts, sheet.devStatus || 'Pending', sheet.devStatus ? 1 : 0)
  })

  return {
    fileId: file?.fileId ?? '',
    totalSheets: sheets.length,
    totalTestCases,
    qaStatusCounts,
    devStatusCounts,
    sheets: sheets.map(sheet => ({
      sheetName: sheet.sheetName,
      module: sheet.module || sheet.purposeOfTesting,
      totalTestCases: sheet.totalTestCases ?? 0,
      qaStatusCounts: {},
      devStatusCounts: {},
    })),
  }
}

export function addCount(target, label, count = 0) {
  if (count > 0) target[label] = (target[label] ?? 0) + count
}
export function rowMatchesSearch(row, search) {
  if (!search.trim()) return true
  const value = search.toLowerCase()
  const searchable = [
    row.testCaseNo,
    row.testCaseId,
    row.module,
    row.description,
    row.actualResult,
    row.issueType,
    row.qaStatus,
    row.devStatus,
    ...(row.rounds ?? []).flatMap(round => [round.qaStatus, round.devStatus, `round ${round.roundNumber}`]),
    ...(row.qaRemarks ?? []),
    ...(row.devRemarks ?? []),
  ]

  return searchable.some(item => String(item ?? '').toLowerCase().includes(value))
}
