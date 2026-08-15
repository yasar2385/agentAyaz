import { useMemo, useState } from 'react'
import './App.css'
import {
  getDashboardCache,
  getOfflineDashboardCache,
  refreshDashboardFile,
  refreshDashboardSheet,
  refreshRegressionIndex,
  syncChangedFiles,
  exportTsv,
  saveDashboardChanges,
  loadSourceUrl,
  downloadToLocal,
  login,
  uploadMasterImport,
  uploadResultImport,
  getImportErrors,
  saveMasterSheetActions,
  commitImportBatch,
  getPlaywrightReadiness,
  getRunMetadata,
  getRunConfigs,
  saveRunConfig,
  triggerRunConfig,
  getRunExecution,
  cancelRunExecution,
  getRecentRuns,
  getRunProgress,
  continueRunConfig,
  verifyFix,
  runReportUrl,
} from './services/testCaseViewerApi'

const SESSION_KEY = 'impact-testcase-viewer-session'
const DEFAULT_FILE_NAME = 'Testcase_2026'
const REPORT_TYPES = {
  master: 'master',
  regression: 'regression',
}
const VIEW_TYPES = {
  sheet: 'sheet',
  module: 'module',
  repeated: 'repeated',
  postponed: 'postponed',
}
const SOURCE_MODES = {
  workspaceCloud: 'workspaceCloud',
  local: 'local',
  hybrid: 'hybrid',
  manual: 'manual',
  playwright: 'playwright',
  startTesting: 'startTesting',
}

function LoginPage({ onLogin }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    if (!username.trim() || !password.trim()) {
      setError('Enter username and password.')
      return
    }

    setLoading(true)
    setError('')
    try {
      const response = await login(username.trim(), password)
      const user = response.user ?? { username: username.trim() }
      localStorage.setItem(SESSION_KEY, JSON.stringify(user))
      onLogin(user)
    } catch (err) {
      setError(err.message || 'Login failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div>
          <p className="eyebrow">Impact QA</p>
          <h1>TestCaseViewer</h1>
          <p className="login-copy">Sign in with your MongoDB users account to review master and regression coverage.</p>
        </div>

        <form className="login-form" onSubmit={submit}>
          <label>
            Username
            <input value={username} onChange={event => setUsername(event.target.value)} autoComplete="username" />
          </label>
          <label>
            Password
            <input
              value={password}
              onChange={event => setPassword(event.target.value)}
              type="password"
              autoComplete="current-password"
            />
          </label>
          {error && <div className="form-error">{error}</div>}
          <button type="submit" disabled={loading}>{loading ? 'Signing in...' : 'Sign in'}</button>
        </form>
      </section>
    </main>
  )
}

function StatusPill({ label, count, tone = 'neutral' }) {
  return (
    <span className={`status-pill ${tone}`}>
      <span>{label}</span>
      {count !== undefined && count !== '' && <strong>{count}</strong>}
    </span>
  )
}

function StatCard({ label, value, hint }) {
  return (
    <article className="stat-card">
      <span>{label}</span>
      <strong>{value}</strong>
      {hint && <small>{hint}</small>}
    </article>
  )
}

function MultiSelect({ label, options, selected, onToggle }) {
  return (
    <div className="filter-block">
      <span>{label}</span>
      <div className="chip-list">
        {options.length === 0 && <small>No values found</small>}
        {options.map(option => (
          <button
            key={option}
            className={selected.includes(option) ? 'chip active' : 'chip'}
            type="button"
            onClick={() => onToggle(option)}
          >
            {option}
          </button>
        ))}
      </div>
    </div>
  )
}

function SelectField({ label, value, options, onChange, placeholder }) {
  return (
    <label className="select-field">
      <span>{label}</span>
      <select value={value} onChange={event => onChange(event.target.value)}>
        <option value="">{placeholder}</option>
        {options.map(option => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
    </label>
  )
}

function statusTone(status) {
  const value = String(status).toLowerCase()
  if (value.includes('pass') || value.includes('closed') || value.includes('fixed')) return 'good'
  if (value.includes('fail') || value.includes('reject') || value.includes('reopen')) return 'bad'
  if (value.includes('wip') || value.includes('clear')) return 'warn'
  return 'neutral'
}

function toggleValue(values, value) {
  return values.includes(value) ? values.filter(item => item !== value) : [...values, value]
}

function uniqueValues(rows, key) {
  return [...new Set(rows.map(row => row[key]).filter(value => String(value ?? '').trim()))].sort((a, b) =>
    String(a).localeCompare(String(b)),
  )
}

function latestRound(row) {
  const rounds = row.rounds ?? []
  return rounds.length > 0 ? rounds[rounds.length - 1] : null
}

function roundLabel(row) {
  const round = latestRound(row)
  return round ? `R${round.roundNumber}` : 'R1'
}

function groupRows(rows, keySelector) {
  return rows.reduce((groups, row) => {
    const key = keySelector(row) || 'Unassigned'
    const existing = groups.get(key) ?? []
    existing.push(row)
    groups.set(key, existing)
    return groups
  }, new Map())
}

function groupEntries(rows, keySelector) {
  return [...groupRows(rows, keySelector).entries()].sort(([a], [b]) => a.localeCompare(b))
}

function statusIsProblem(status) {
  const value = String(status ?? '').toLowerCase()
  return ['fail', 'failed', 'reject', 'reopen', 'error', 'bug'].some(term => value.includes(term))
}

function isRepeatedRow(row, duplicateKeys) {
  const identity = row.testCaseId || row.testCaseNo
  return (
    (identity && duplicateKeys.has(identity)) ||
    statusIsProblem(row.qaStatus) ||
    statusIsProblem(row.devStatus) ||
    statusIsProblem(row.issueType)
  )
}

function isPostponedRow(row) {
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

function cacheFileToOption(file) {
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

function cacheSheetToInfo(sheet, index) {
  return {
    name: sheet.sheetName,
    index,
    rowCount: sheet.totalTestCases ?? 0,
    columnCount: 0,
  }
}

function cacheSheetToRowsResponse(fileId, sheet) {
  return {
    fileId,
    sheetName: sheet?.sheetName ?? '',
    rows: sheet?.rows ?? [],
    qaStatuses: uniqueValues(sheet?.rows ?? [], 'qaStatus'),
    devStatuses: uniqueValues(sheet?.rows ?? [], 'devStatus'),
  }
}

function cacheFileToSummary(file) {
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

function addCount(target, label, count = 0) {
  if (count > 0) target[label] = (target[label] ?? 0) + count
}

function ImportSummary({ batch }) {
  if (!batch) return null
  return (
    <section className="stats-grid import-stats">
      <StatCard label="Sheets/pages" value={batch.sheetsDetected ?? 0} hint="detected" />
      <StatCard label="New" value={batch.newSheets ?? 0} hint="safe to import" />
      <StatCard label="Existing" value={batch.existingSheets ?? 0} hint="needs action" />
      <StatCard label="Rows ready" value={(batch.rowsAdded ?? 0) + (batch.rowsUpdated ?? 0)} hint="dry-run" />
      <StatCard label="Errors" value={batch.rowsError ?? 0} hint={batch.status ?? 'DRY_RUN'} />
      <StatCard label="Source" value={batch.sourceType || 'TSV'} hint="format" />
    </section>
  )
}

function ManualUploadPanel({ user, loading, onSetLoading, onError, onCommitted }) {
  const [masterFile, setMasterFile] = useState(null)
  const [resultFiles, setResultFiles] = useState([])
  const [resultMode, setResultMode] = useState('single')
  const [masterBatch, setMasterBatch] = useState(null)
  const [resultBatch, setResultBatch] = useState(null)
  const [errors, setErrors] = useState([])
  const [ackErrors, setAckErrors] = useState(false)
  const [message, setMessage] = useState('')

  async function runMasterUpload(event) {
    event.preventDefault()
    if (!masterFile) return
    await runImport(async () => {
      const batch = await uploadMasterImport(masterFile, user)
      setMasterBatch(batch)
      setResultBatch(null)
      setAckErrors(false)
      setErrors(batch.errors ?? (batch.rowsError ? await getImportErrors(batch.batchId, user) : []))
      setMessage('Master upload dry-run is ready.')
    })
  }

  async function runResultUpload(event) {
    event.preventDefault()
    if (resultFiles.length === 0) return
    await runImport(async () => {
      const batch = await uploadResultImport(resultFiles, resultMode, user)
      setResultBatch(batch)
      setMasterBatch(null)
      setAckErrors(false)
      setErrors(batch.errors ?? (batch.rowsError ? await getImportErrors(batch.batchId, user) : []))
      setMessage('Result upload dry-run is ready.')
    })
  }

  async function runImport(action) {
    onSetLoading(true)
    onError('')
    setMessage('')
    try {
      await action()
    } catch (err) {
      onError(err.message || 'Import failed.')
    } finally {
      onSetLoading(false)
    }
  }

  async function updateSheetAction(sheetId, action) {
    if (!masterBatch) return
    const actions = masterBatch.sheets
      .filter(sheet => sheet.conflictStatus === 'EXISTS')
      .map(sheet => ({
        sheetId: sheet.id,
        action: sheet.id === sheetId ? action : sheet.selectedAction,
      }))
    await runImport(async () => {
      const batch = await saveMasterSheetActions(masterBatch.batchId, actions, user)
      setMasterBatch(batch)
      setMessage('Sheet/page action saved.')
    })
  }

  async function commitBatch(batch) {
    if (!batch) return
    await runImport(async () => {
      const committed = await commitImportBatch(batch.batchId, user)
      setMessage(`Batch ${committed.batchId} committed.`)
      if (committed.uploadKind === 'master') setMasterBatch(committed)
      if (committed.uploadKind === 'result') setResultBatch(committed)
      await onCommitted(committed.uploadKind === 'result' ? REPORT_TYPES.regression : REPORT_TYPES.master)
    })
  }

  const activeBatch = masterBatch || resultBatch
  const hasErrors = (activeBatch?.rowsError ?? 0) > 0
  const unresolvedMasterConflicts = (masterBatch?.sheets ?? [])
    .filter(sheet => sheet.conflictStatus === 'EXISTS')
    .some(sheet => !['OVERWRITE', 'SKIP'].includes(sheet.selectedAction))
  const canCommitMaster = masterBatch && masterBatch.status !== 'COMMITTED' && !unresolvedMasterConflicts && !hasErrors
  const canCommitResult = resultBatch && resultBatch.status !== 'COMMITTED' && !hasErrors

  return (
    <section className="manual-upload-panel">
      <div className="upload-grid">
        <form className="upload-card" onSubmit={runMasterUpload}>
          <div>
            <h2>Master Test Case Upload</h2>
            <p>CSV/TSV with Sheet Name, Module/Sub Module, and Test Case ID columns.</p>
          </div>
          <input type="file" accept=".xlsx,.csv,.tsv,text/csv,text/tab-separated-values" onChange={event => setMasterFile(event.target.files?.[0] ?? null)} />
          <button type="submit" disabled={loading || !masterFile}>Dry-run master upload</button>
        </form>

        <form className="upload-card" onSubmit={runResultUpload}>
          <div>
            <h2>Test Result Upload</h2>
            <p>Upload one single result file or multiple regression result files.</p>
          </div>
          <div className="segmented compact">
            <button type="button" className={resultMode === 'single' ? 'active' : ''} onClick={() => setResultMode('single')}>Single</button>
            <button type="button" className={resultMode === 'regression' ? 'active' : ''} onClick={() => setResultMode('regression')}>Regression</button>
          </div>
          <input
            type="file"
            multiple={resultMode === 'regression'}
            accept=".xlsx,.csv,.tsv,text/csv,text/tab-separated-values"
            onChange={event => setResultFiles([...(event.target.files ?? [])])}
          />
          <button type="submit" disabled={loading || resultFiles.length === 0}>Dry-run result upload</button>
        </form>
      </div>

      {message && <section className="notice compact">{message}</section>}

      <ImportSummary batch={activeBatch} />

      {masterBatch && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Sheet/Page Conflict Review</h2>
            <span>{masterBatch.status}</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Sheet/Page</th>
                  <th>Module</th>
                  <th>Rows</th>
                  <th>Status</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {masterBatch.sheets.map(sheet => (
                  <tr key={sheet.id}>
                    <td>{sheet.sheetName}</td>
                    <td>{sheet.moduleName}</td>
                    <td>{sheet.rowCount}</td>
                    <td><StatusPill label={sheet.conflictStatus} tone={sheet.conflictStatus === 'EXISTS' ? 'warn' : 'good'} /></td>
                    <td>
                      {sheet.conflictStatus === 'EXISTS' ? (
                        <select value={sheet.selectedAction} onChange={event => updateSheetAction(sheet.id, event.target.value)} disabled={loading || masterBatch.status === 'COMMITTED'}>
                          <option value="">Choose</option>
                          <option value="OVERWRITE">Overwrite</option>
                          <option value="SKIP">Skip</option>
                        </select>
                      ) : (
                        <StatusPill label={sheet.selectedAction || 'Import'} tone="good" />
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button type="button" onClick={() => commitBatch(masterBatch)} disabled={loading || !canCommitMaster}>
            Commit master import
          </button>
        </section>
      )}

      {resultBatch && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Result Dry-run</h2>
            <span>{resultBatch.status}</span>
          </div>
          <button type="button" onClick={() => commitBatch(resultBatch)} disabled={loading || !canCommitResult}>
            Commit result import
          </button>
        </section>
      )}

      {errors.length > 0 && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Upload Errors</h2>
            <label className="ack-field">
              <input type="checkbox" checked={ackErrors} onChange={event => setAckErrors(event.target.checked)} />
              Acknowledge
            </label>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Row</th>
                  <th>Value</th>
                  <th>Error</th>
                </tr>
              </thead>
              <tbody>
                {errors.map(error => (
                  <tr key={error.id}>
                    <td>{error.rowNumber}</td>
                    <td>{error.rawValue}</td>
                    <td>{error.errorMessage}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </section>
  )
}

function isRunManager(user) {
  const role = typeof user?.role === 'string' ? user.role : JSON.stringify(user?.role ?? '')
  return role.toLowerCase().includes('dev') || role.toLowerCase().includes('manager')
}

function PlaywrightRunsPanel({ user, loading, onSetLoading, onError }) {
  const [readiness, setReadiness] = useState(null)
  const [metadata, setMetadata] = useState({ modules: [], testingTypes: [], roles: [], clients: [] })
  const [configs, setConfigs] = useState([])
  const [execution, setExecution] = useState(null)
  const [message, setMessage] = useState('')
  const [form, setForm] = useState({
    testingName: '',
    description: '',
    modules: [],
    testingTypes: [],
    roleBased: 'ALL',
    roleBasedClient: 'ALL',
    ui: 'off',
  })
  const canManage = isRunManager(user)

  async function loadRuns() {
    await runAction(async () => {
      const [ready, meta, saved] = await Promise.all([
        getPlaywrightReadiness(user),
        getRunMetadata(user),
        getRunConfigs(user),
      ])
      setReadiness(ready)
      setMetadata(meta)
      setConfigs(saved)
    })
  }

  async function runAction(action) {
    onSetLoading(true)
    onError('')
    setMessage('')
    try {
      await action()
    } catch (err) {
      onError(err.message || 'Playwright run action failed.')
    } finally {
      onSetLoading(false)
    }
  }

  async function saveConfig(event) {
    event.preventDefault()
    await runAction(async () => {
      await saveRunConfig(form, user)
      setConfigs(await getRunConfigs(user))
      setMessage('Run config saved.')
      setForm(current => ({ ...current, testingName: '', description: '' }))
    })
  }

  async function triggerConfig(configId) {
    await runAction(async () => {
      const started = await triggerRunConfig(configId, user)
      setExecution(started)
      setMessage(`Execution ${started.id} queued.`)
    })
  }

  async function pollExecution() {
    if (!execution?.id) return
    await runAction(async () => {
      setExecution(await getRunExecution(execution.id, user))
    })
  }

  async function cancelExecution() {
    if (!execution?.id) return
    await runAction(async () => {
      setExecution(await cancelRunExecution(execution.id, user))
    })
  }

  function toggleListValue(field, value) {
    setForm(current => ({
      ...current,
      [field]: current[field].includes(value)
        ? current[field].filter(item => item !== value)
        : [...current[field], value],
    }))
  }

  const blockers = readiness?.blockingIssues ?? []
  const fullRun = form.modules.length === 0
    && form.testingTypes.length === 0
    && form.roleBased === 'ALL'
    && form.roleBasedClient === 'ALL'
    && form.ui !== 'on'
  const terminal = ['PASSED', 'FAILED', 'ERROR', 'CANCELLED'].includes(execution?.status)

  return (
    <section className="manual-upload-panel">
      <section className="upload-review">
        <div className="section-heading">
          <h2>Playwright Readiness</h2>
          <button className="secondary" type="button" onClick={loadRuns} disabled={loading}>Check</button>
        </div>
        {!readiness && <div className="notice compact">Check readiness before creating or running configs.</div>}
        {readiness && (
          <>
            <div className="readiness-grid">
              <StatusPill label="Project" tone={readiness.playwrightProjectFound ? 'good' : 'bad'} />
              <StatusPill label="Tags" tone={readiness.taggedSpecsFound ? 'good' : 'bad'} />
              <StatusPill label="Node" tone={readiness.nodeAvailable ? 'good' : 'bad'} />
              <StatusPill label="Playwright" tone={readiness.playwrightAvailable ? 'good' : 'bad'} />
              <StatusPill label="Browsers" tone={readiness.browsersAvailable ? 'good' : 'warn'} />
              <StatusPill label="Master data" tone={readiness.manualMasterDataAvailable ? 'good' : 'bad'} />
            </div>
            {blockers.length > 0 && (
              <div className="notice error compact">
                {blockers.map(issue => <div key={issue}>{issue}</div>)}
              </div>
            )}
            {blockers.length === 0 && <div className="notice compact">Ready to create and trigger named runs.</div>}
          </>
        )}
      </section>

      <section className="upload-review">
        <div className="section-heading">
          <h2>Named Run Config</h2>
          {fullRun && <StatusPill label="Full run" tone="warn" />}
        </div>
        <form className="run-config-form" onSubmit={saveConfig}>
          <label className="source-url-field">
            <span>Testing name</span>
            <input value={form.testingName} onChange={event => setForm({ ...form, testingName: event.target.value })} />
          </label>
          <label className="source-url-field">
            <span>Description</span>
            <input value={form.description} onChange={event => setForm({ ...form, description: event.target.value })} />
          </label>
          <div className="filter-block">
            <span>Module/Sub Module</span>
            <div className="chip-list">
              <button type="button" className={form.modules.length === 0 ? 'chip active' : 'chip'} onClick={() => setForm({ ...form, modules: [] })}>All modules</button>
              {metadata.modules.map(module => (
                <button type="button" key={module} className={form.modules.includes(module) ? 'chip active' : 'chip'} onClick={() => toggleListValue('modules', module)}>{module}</button>
              ))}
            </div>
          </div>
          <div className="filter-block">
            <span>Type of testing</span>
            <div className="chip-list">
              <button type="button" className={form.testingTypes.length === 0 ? 'chip active' : 'chip'} onClick={() => setForm({ ...form, testingTypes: [] })}>All types</button>
              {metadata.testingTypes.map(type => (
                <button type="button" key={type} className={form.testingTypes.includes(type) ? 'chip active' : 'chip'} onClick={() => toggleListValue('testingTypes', type)}>{type}</button>
              ))}
            </div>
          </div>
          <SelectField label="Role" value={form.roleBased} options={[{ value: 'ALL', label: 'All roles' }, ...metadata.roles.map(role => ({ value: role, label: role }))]} onChange={value => setForm({ ...form, roleBased: value })} placeholder="Role" />
          <SelectField label="Client" value={form.roleBasedClient} options={[{ value: 'ALL', label: 'All clients' }, ...metadata.clients.map(client => ({ value: client, label: client }))]} onChange={value => setForm({ ...form, roleBasedClient: value })} placeholder="Client" />
          <div className="segmented compact">
            <button type="button" className={form.ui === 'off' ? 'active' : ''} onClick={() => setForm({ ...form, ui: 'off' })}>Headless</button>
            <button type="button" className={form.ui === 'on' ? 'active' : ''} onClick={() => setForm({ ...form, ui: 'on' })}>Headed</button>
          </div>
          <button type="submit" disabled={loading || !canManage || !form.testingName.trim() || blockers.length > 0}>Save config</button>
        </form>
      </section>

      <section className="upload-review">
        <div className="section-heading">
          <h2>Saved Configs</h2>
          <span>{configs.length}</span>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Targets</th>
                <th>Flags</th>
                <th>Run</th>
              </tr>
            </thead>
            <tbody>
              {configs.map(config => (
                <tr key={config.id}>
                  <td>{config.testingName}{config.isFullRun && <div><StatusPill label="Full run" tone="warn" /></div>}</td>
                  <td>{(config.modules.length ? config.modules : ['All modules']).join(', ')} / {(config.testingTypes.length ? config.testingTypes : ['All types']).join(', ')}</td>
                  <td>Role {config.roleBased}; Client {config.roleBasedClient}; UI {config.ui}</td>
                  <td><button type="button" disabled={loading || !canManage || blockers.length > 0} onClick={() => triggerConfig(config.id)}>Run</button></td>
                </tr>
              ))}
              {configs.length === 0 && <tr><td colSpan="4" className="empty-cell">No saved configs yet.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      {execution && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Execution {execution.id}</h2>
            <StatusPill label={execution.status} tone={execution.status === 'PASSED' ? 'good' : execution.status === 'FAILED' || execution.status === 'ERROR' ? 'bad' : 'warn'} />
          </div>
          <div className="notice compact">{execution.failureSummary || execution.playwrightCommand}</div>
          <div className="topbar-actions">
            {!terminal && <button className="secondary" type="button" onClick={pollExecution} disabled={loading}>Refresh status</button>}
            {!terminal && canManage && <button type="button" onClick={cancelExecution} disabled={loading}>Cancel</button>}
            {['PASSED', 'FAILED'].includes(execution.status) && <a className="button-link" href={runReportUrl(execution.id)} target="_blank" rel="noreferrer">Open report</a>}
          </div>
        </section>
      )}

      {message && <section className="notice compact">{message}</section>}
    </section>
  )
}

function StartTestingPanel({ user, loading, onSetLoading, onError }) {
  const [readiness, setReadiness] = useState(null)
  const [metadata, setMetadata] = useState({ modules: [], testingTypes: [], roles: [], clients: [], contentTypes: [], domains: [], roleWorkflows: [], testingUrls: [], refStyles: [] })
  const [configs, setConfigs] = useState([])
  const [recentScope, setRecentScope] = useState('mine')
  const [recentRuns, setRecentRuns] = useState([])
  const [progressByConfig, setProgressByConfig] = useState({})
  const [execution, setExecution] = useState(null)
  const [verifyResult, setVerifyResult] = useState(null)
  const [message, setMessage] = useState('')
  const [form, setForm] = useState({
    testingName: '',
    description: '',
    modules: [],
    testingTypes: [],
    roleBased: 'ALL',
    roleBasedClient: 'ALL',
    ui: 'off',
    client: 'ALL',
    contentType: 'books',
    domain: 'UAT',
    roleWorkflow: 'author_editor_collator',
    testingUrl: 'author',
    mantisTicket: '',
    refStyle: 'number',
  })
  const [verifyForm, setVerifyForm] = useState({ testCaseId: '', mantisTicket: '' })
  const canManage = isRunManager(user)
  const blockers = readiness?.blockingIssues ?? []

  async function runAction(action) {
    onSetLoading(true)
    onError('')
    setMessage('')
    try {
      await action()
    } catch (err) {
      onError(err.message || 'Start Testing action failed.')
    } finally {
      onSetLoading(false)
    }
  }

  async function loadStartTesting(scope = recentScope) {
    await runAction(async () => {
      const [ready, meta, saved, recent] = await Promise.all([
        getPlaywrightReadiness(user),
        getRunMetadata(user),
        getRunConfigs(user),
        getRecentRuns(scope, 20, user),
      ])
      setReadiness(ready)
      setMetadata(meta)
      setConfigs(saved)
      setRecentRuns(recent.runs ?? [])
      const progressEntries = await Promise.all(saved.map(async config => [config.id, await getRunProgress(config.id, user)]))
      setProgressByConfig(Object.fromEntries(progressEntries))
    })
  }

  async function changeRecentScope(scope) {
    setRecentScope(scope)
    await runAction(async () => {
      const recent = await getRecentRuns(scope, 20, user)
      setRecentRuns(recent.runs ?? [])
    })
  }

  async function saveConfig(event) {
    event.preventDefault()
    await runAction(async () => {
      await saveRunConfig(form, user)
      setMessage('Testing config saved.')
      await loadStartTesting(recentScope)
      setForm(current => ({ ...current, testingName: '', description: '', mantisTicket: '' }))
    })
  }

  async function continueConfig(configId) {
    await runAction(async () => {
      const started = await continueRunConfig(configId, user)
      setExecution(started)
      setMessage(`Continuing ${started.moduleName || 'next module'} in execution ${started.id}.`)
      await loadStartTesting(recentScope)
    })
  }

  async function submitVerify(event) {
    event.preventDefault()
    await runAction(async () => {
      const started = await verifyFix(verifyForm, user)
      setVerifyResult(started)
      setExecution(started)
      setMessage(`Bug verification ${started.id} queued.`)
    })
  }

  function toggleListValue(field, value) {
    setForm(current => ({
      ...current,
      [field]: current[field].includes(value)
        ? current[field].filter(item => item !== value)
        : [...current[field], value],
    }))
  }

  const fullRun = form.modules.length === 0
    && form.testingTypes.length === 0
    && form.roleBased === 'ALL'
    && form.roleBasedClient === 'ALL'
    && form.ui !== 'on'
  const reportStatuses = ['PASSED', 'FAILED']

  return (
    <section className="manual-upload-panel">
      <section className="upload-review">
        <div className="section-heading">
          <h2>Start Testing</h2>
          <button className="secondary" type="button" onClick={() => loadStartTesting()} disabled={loading}>Refresh</button>
        </div>
        {!readiness && <div className="notice compact">Refresh to check Playwright readiness, saved configs, progress, and recent runs.</div>}
        {readiness && (
          <>
            <div className="readiness-grid">
              <StatusPill label="Repo" tone={readiness.playwrightProjectFound ? 'good' : 'bad'} />
              <StatusPill label="@testcase" tone={blockers.some(issue => issue.includes('@testcase=')) ? 'bad' : 'good'} />
              <StatusPill label="Master data" tone={readiness.manualMasterDataAvailable ? 'good' : 'bad'} />
              <StatusPill label="Role gate" tone={readiness.roleGateAvailable ? 'good' : 'bad'} />
            </div>
            {blockers.length > 0 && <div className="notice error compact">{blockers.map(issue => <div key={issue}>{issue}</div>)}</div>}
          </>
        )}
      </section>

      <section className="upload-review">
        <div className="section-heading">
          <h2>Recent Runs</h2>
          <div className="segmented compact">
            <button type="button" className={recentScope === 'mine' ? 'active' : ''} onClick={() => changeRecentScope('mine')}>Mine</button>
            <button type="button" className={recentScope === 'team' ? 'active' : ''} onClick={() => changeRecentScope('team')}>Team</button>
          </div>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Name</th><th>Status</th><th>Kind</th><th>Module</th><th>By</th><th>Time</th><th>Report</th></tr>
            </thead>
            <tbody>
              {recentRuns.map(run => (
                <tr key={run.id}>
                  <td>{run.testingName || run.testCaseId || run.playwrightCommand}</td>
                  <td><StatusPill label={run.status} tone={run.status === 'PASSED' ? 'good' : run.status === 'FAILED' || run.status === 'ERROR' ? 'bad' : 'warn'} /></td>
                  <td>{run.runKind}</td>
                  <td>{run.moduleName || 'All modules'}</td>
                  <td>{run.triggeredBy}</td>
                  <td>{new Date(run.triggeredAt).toLocaleString()}</td>
                  <td>{reportStatuses.includes(run.status) ? <a href={runReportUrl(run.id)} target="_blank" rel="noreferrer">Open</a> : ''}</td>
                </tr>
              ))}
              {recentRuns.length === 0 && <tr><td colSpan="7" className="empty-cell">No recent runs found.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      <section className="upload-review">
        <div className="section-heading">
          <h2>Continue Testing</h2>
          <span>{configs.length}</span>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Config</th><th>Last module</th><th>Next module</th><th>Action</th></tr>
            </thead>
            <tbody>
              {configs.map(config => {
                const progress = progressByConfig[config.id] ?? {}
                return (
                  <tr key={config.id}>
                    <td>{config.testingName}</td>
                    <td>{progress.lastModuleName || 'Not started'}</td>
                    <td>{progress.nextModuleName || 'Complete'}</td>
                    <td><button type="button" disabled={loading || !canManage || blockers.length > 0 || !progress.nextModuleName} onClick={() => continueConfig(config.id)}>Continue</button></td>
                  </tr>
                )
              })}
              {configs.length === 0 && <tr><td colSpan="4" className="empty-cell">No saved configs yet.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      <section className="upload-review">
        <div className="section-heading">
          <h2>Verify Bug Fix</h2>
          {verifyResult?.fixSignal && <StatusPill label={verifyResult.fixSignal} tone={verifyResult.fixSignal === 'Fixed' ? 'good' : verifyResult.fixSignal === 'Still Failing' ? 'bad' : 'warn'} />}
        </div>
        <form className="run-config-form" onSubmit={submitVerify}>
          <label className="source-url-field"><span>Test case ID</span><input value={verifyForm.testCaseId} onChange={event => setVerifyForm({ ...verifyForm, testCaseId: event.target.value })} /></label>
          <label className="source-url-field"><span>Mantis ticket</span><input value={verifyForm.mantisTicket} onChange={event => setVerifyForm({ ...verifyForm, mantisTicket: event.target.value })} /></label>
          <button type="submit" disabled={loading || !canManage || !verifyForm.testCaseId.trim() || blockers.length > 0}>Verify</button>
        </form>
      </section>

      <section className="upload-review">
        <div className="section-heading">
          <h2>New Run</h2>
          {fullRun && <StatusPill label="Full run" tone="warn" />}
        </div>
        <form className="run-config-form" onSubmit={saveConfig}>
          <label className="source-url-field"><span>Testing name</span><input value={form.testingName} onChange={event => setForm({ ...form, testingName: event.target.value })} /></label>
          <label className="source-url-field"><span>Description</span><input value={form.description} onChange={event => setForm({ ...form, description: event.target.value })} /></label>
          <div className="filter-block">
            <span>Module/Sub Module</span>
            <div className="chip-list">
              <button type="button" className={form.modules.length === 0 ? 'chip active' : 'chip'} onClick={() => setForm({ ...form, modules: [] })}>All modules</button>
              {metadata.modules.map(module => <button type="button" key={module} className={form.modules.includes(module) ? 'chip active' : 'chip'} onClick={() => toggleListValue('modules', module)}>{module}</button>)}
            </div>
          </div>
          <div className="filter-block">
            <span>Type of testing</span>
            <div className="chip-list">
              <button type="button" className={form.testingTypes.length === 0 ? 'chip active' : 'chip'} onClick={() => setForm({ ...form, testingTypes: [] })}>All types</button>
              {metadata.testingTypes.map(type => <button type="button" key={type} className={form.testingTypes.includes(type) ? 'chip active' : 'chip'} onClick={() => toggleListValue('testingTypes', type)}>{type}</button>)}
            </div>
          </div>
          <SelectField label="Role" value={form.roleBased} options={[{ value: 'ALL', label: 'All roles' }, ...metadata.roles.map(role => ({ value: role, label: role }))]} onChange={value => setForm({ ...form, roleBased: value })} placeholder="Role" />
          <SelectField label="Client filter" value={form.roleBasedClient} options={[{ value: 'ALL', label: 'All clients' }, ...metadata.clients.map(client => ({ value: client, label: client }))]} onChange={value => setForm({ ...form, roleBasedClient: value })} placeholder="Client filter" />
          <SelectField label="Client" value={form.client} options={[{ value: 'ALL', label: 'All clients' }, ...(metadata.clients.length ? metadata.clients : ['oup', 'lww', 'oso', 'oho', 'tnf']).map(client => ({ value: client, label: client }))]} onChange={value => setForm({ ...form, client: value })} placeholder="Client" />
          <SelectField label="Content type" value={form.contentType} options={(metadata.contentTypes.length ? metadata.contentTypes : ['books', 'journal']).map(value => ({ value, label: value }))} onChange={value => setForm({ ...form, contentType: value })} placeholder="Content type" />
          <SelectField label="Domain" value={form.domain} options={(metadata.domains.length ? metadata.domains : ['UAT', 'UAT_QA', 'DEV', 'DEV_QA', 'LIVE', 'PROD']).map(value => ({ value, label: value }))} onChange={value => setForm({ ...form, domain: value })} placeholder="Domain" />
          <SelectField label="Role workflow" value={form.roleWorkflow} options={(metadata.roleWorkflows.length ? metadata.roleWorkflows : ['author_editor_collator']).map(value => ({ value, label: value }))} onChange={value => setForm({ ...form, roleWorkflow: value })} placeholder="Role workflow" />
          <SelectField label="Testing URL" value={form.testingUrl} options={(metadata.testingUrls.length ? metadata.testingUrls : ['author']).map(value => ({ value, label: value }))} onChange={value => setForm({ ...form, testingUrl: value })} placeholder="Testing URL" />
          <label className="source-url-field"><span>Mantis ticket</span><input value={form.mantisTicket} onChange={event => setForm({ ...form, mantisTicket: event.target.value })} /></label>
          <SelectField label="Reference style" value={form.refStyle} options={(metadata.refStyles.length ? metadata.refStyles : ['number']).map(value => ({ value, label: value }))} onChange={value => setForm({ ...form, refStyle: value })} placeholder="Reference style" />
          <div className="segmented compact">
            <button type="button" className={form.ui === 'off' ? 'active' : ''} onClick={() => setForm({ ...form, ui: 'off' })}>Headless</button>
            <button type="button" className={form.ui === 'on' ? 'active' : ''} onClick={() => setForm({ ...form, ui: 'on' })}>Headed</button>
          </div>
          <button type="submit" disabled={loading || !canManage || !form.testingName.trim() || blockers.length > 0}>Save config</button>
        </form>
      </section>

      {execution && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Execution {execution.id}</h2>
            <StatusPill label={execution.status} tone={execution.status === 'PASSED' ? 'good' : execution.status === 'FAILED' || execution.status === 'ERROR' ? 'bad' : 'warn'} />
          </div>
          <div className="notice compact">{execution.failureSummary || execution.playwrightCommand}</div>
          {reportStatuses.includes(execution.status) && <a className="button-link" href={runReportUrl(execution.id)} target="_blank" rel="noreferrer">Open report</a>}
        </section>
      )}

      {message && <section className="notice compact">{message}</section>}
    </section>
  )
}

function rowMatchesSearch(row, search) {
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

function RowTable({ rows, rowsLoading, emptyMessage }) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>TC No.</th>
            <th>TC ID</th>
            <th>Module</th>
            <th>Description</th>
            <th>Round</th>
            <th>QA Status</th>
            <th>Dev. Status</th>
            <th>Issue Type</th>
            <th>Actual Result</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => {
            const round = latestRound(row)
            const qaStatus = round?.qaStatus || row.qaStatus || ''
            const devStatus = round?.devStatus || row.devStatus || ''

            return (
              <tr key={`${row.sheetName}-${row.testCaseId}-${row.testCaseNo}-${index}`}>
                <td>{row.testCaseNo}</td>
                <td>{row.testCaseId}</td>
                <td>{row.module}</td>
                <td>{row.description}</td>
                <td><StatusPill label={roundLabel(row)} /></td>
                <td><StatusPill label={qaStatus || 'Blank'} tone={statusTone(qaStatus)} /></td>
                <td><StatusPill label={devStatus || 'Blank'} tone={statusTone(devStatus)} /></td>
                <td>{row.issueType}</td>
                <td>{row.actualResult}</td>
              </tr>
            )
          })}
          {!rowsLoading && rows.length === 0 && (
            <tr>
              <td colSpan="9" className="empty-cell">{emptyMessage}</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function DashboardPage({ user, onLogout }) {
  const [reportType, setReportType] = useState(REPORT_TYPES.master)
  const [, setCacheByReport] = useState({ master: null, regression: null })
  const [knownFile, setKnownFile] = useState(null)
  const [masterFiles, setMasterFiles] = useState([])
  const [regressionFiles, setRegressionFiles] = useState([])
  const [selectedFileId, setSelectedFileId] = useState('')
  const [selectedFile, setSelectedFile] = useState(null)
  const [sheets, setSheets] = useState([])
  const [summary, setSummary] = useState(null)
  const [selectedSheet, setSelectedSheet] = useState('')
  const [sheetRows, setSheetRows] = useState(null)
  const [allSheetRows, setAllSheetRows] = useState([])
  const [activeView, setActiveView] = useState(VIEW_TYPES.sheet)
  const [search, setSearch] = useState('')
  const [qaFilters, setQaFilters] = useState([])
  const [devFilters, setDevFilters] = useState([])
  const [issueFilters, setIssueFilters] = useState([])
  const [moduleFilters, setModuleFilters] = useState([])
  const [loading, setLoading] = useState(false)
  const [rowsLoading, setRowsLoading] = useState(false)
  const [error, setError] = useState('')
  const [dashboardLoaded, setDashboardLoaded] = useState(false)
  const [sourceMode, setSourceMode] = useState(SOURCE_MODES.workspaceCloud)
  const [sourceUrl, setSourceUrl] = useState('')

  async function loadDashboard(nextReportType = reportType) {
    setLoading(true)
    setError('')
    clearSelection()

    try {
      const [masterCache, regressionCache] = await Promise.all([
        sourceMode !== SOURCE_MODES.workspaceCloud
          ? getOfflineDashboardCache(REPORT_TYPES.master, user)
          : getDashboardCache(REPORT_TYPES.master, user),
        sourceMode !== SOURCE_MODES.workspaceCloud
          ? getOfflineDashboardCache(REPORT_TYPES.regression, user)
          : getDashboardCache(REPORT_TYPES.regression, user),
      ])

      setCacheByReport({ master: masterCache, regression: regressionCache })
      const masters = masterCache.files.map(cacheFileToOption)
      const regressions = regressionCache.files.map(cacheFileToOption)
      setKnownFile(masters[0] ?? { id: '', name: DEFAULT_FILE_NAME, sheets: [], scanStatus: 'NotStarted' })
      setMasterFiles(masters)
      setRegressionFiles(regressions)

      if (nextReportType === REPORT_TYPES.master) {
        loadCachedFile(masters[0] ?? null)
      } else {
        const firstRegression = regressions[0] ?? null
        if (firstRegression) {
          loadCachedFile(firstRegression)
        }
      }
    } catch (err) {
      setError(err.message || 'Unable to load dashboard.')
    } finally {
      setDashboardLoaded(true)
      setLoading(false)
    }
  }

  function clearSelection() {
    setSelectedFileId('')
    setSelectedFile(null)
    setSheets([])
    setSummary(null)
    setSelectedSheet('')
    setSheetRows(null)
    setAllSheetRows([])
    setActiveView(VIEW_TYPES.sheet)
    clearFilters()
  }

  function clearFilters() {
    setSearch('')
    setQaFilters([])
    setDevFilters([])
    setIssueFilters([])
    setModuleFilters([])
  }

  async function loadFile(file) {
    loadCachedFile(file)
  }

  function loadCachedFile(file) {
    if (!file?.id) return
    setSelectedFile(file)
    setSelectedFileId(file.id)
    const cachedSheets = file.sheets ?? []
    const sheetList = cachedSheets.map(cacheSheetToInfo)
    const dashboardSummary = cacheFileToSummary({
      fileId: file.id,
      sheets: cachedSheets,
    })
    const firstSheet = sheetList[0]?.name ?? ''

    setSheets(sheetList)
    setSummary(dashboardSummary)
    setSelectedSheet(firstSheet)
    setSheetRows(firstSheet ? cacheSheetToRowsResponse(file.id, cachedSheets[0]) : null)
    setAllSheetRows(cachedSheets.flatMap(sheet => sheet.rows ?? []))
    setActiveView(VIEW_TYPES.sheet)
    clearFilters()
  }

  function applyCacheResponse(cacheResponse, nextReportType = reportType, nextFileId = selectedFileId) {
    setCacheByReport(current => ({ ...current, [nextReportType]: cacheResponse }))
    const files = cacheResponse.files.map(cacheFileToOption)

    if (nextReportType === REPORT_TYPES.master) {
      setKnownFile(files[0] ?? { id: '', name: DEFAULT_FILE_NAME, sheets: [], scanStatus: 'NotStarted' })
      setMasterFiles(files)
    } else {
      setRegressionFiles(files)
    }

    const nextFile = files.find(file => file.id === nextFileId) ?? files[0] ?? null
    if (nextReportType === reportType) {
      loadCachedFile(nextFile)
    }
  }

  async function refreshCurrentIndex() {
    setLoading(true)
    setError('')
    try {
      const response = reportType === REPORT_TYPES.regression
        ? await refreshRegressionIndex(user)
        : await refreshDashboardFile({ reportType: REPORT_TYPES.master, fileName: DEFAULT_FILE_NAME }, user)
      applyCacheResponse(response)
    } catch (err) {
      setError(err.message || 'Unable to refresh index.')
    } finally {
      setLoading(false)
    }
  }

  async function refreshSelectedSheet() {
    if (!selectedFileId || !selectedSheet) return
    setRowsLoading(true)
    setError('')
    try {
      const response = await refreshDashboardSheet({
        reportType,
        fileId: selectedFileId,
        fileName: selectedFile?.name ?? '',
        sheetName: selectedSheet,
      }, user)
      applyCacheResponse(response, reportType, selectedFileId)
      setActiveView(VIEW_TYPES.sheet)
    } catch (err) {
      setError(err.message || 'Unable to refresh selected sheet.')
    } finally {
      setRowsLoading(false)
    }
  }

  async function analyzeSelectedRegressionFile() {
    if (!selectedFileId) return
    setLoading(true)
    setError('')
    try {
      const response = await refreshDashboardFile({
        reportType: REPORT_TYPES.regression,
        fileId: selectedFileId,
        fileName: selectedFile?.name ?? '',
      }, user)
      applyCacheResponse(response, REPORT_TYPES.regression, selectedFileId)
    } catch (err) {
      setError(err.message || 'Unable to analyze regression file.')
    } finally {
      setLoading(false)
    }
  }

  async function syncCurrentChangedFiles() {
    setLoading(true)
    setError('')
    try {
      const response = await syncChangedFiles(reportType, user)
      applyCacheResponse(response)
    } catch (err) {
      setError(err.message || 'Unable to sync changed files.')
    } finally {
      setLoading(false)
    }
  }

  async function exportCurrentTsv() {
    setLoading(true)
    setError('')
    try {
      const response = await exportTsv(reportType, user)
      applyCacheResponse(response)
    } catch (err) {
      setError(err.message || 'Unable to export TSV.')
    } finally {
      setLoading(false)
    }
  }

  async function saveCurrentChanges() {
    if (!selectedFileId) return
    setLoading(true)
    setError('')
    try {
      const response = await saveDashboardChanges({
        reportType,
        fileId: selectedFileId,
        fileName: selectedFile?.name ?? '',
        sheetName: selectedSheet,
        edits: [],
      }, user)
      applyCacheResponse(response, reportType, selectedFileId)
    } catch (err) {
      setError(err.message || 'Unable to save changes.')
    } finally {
      setLoading(false)
    }
  }

  async function changeReportType(nextReportType) {
    setReportType(nextReportType)
    setError('')
    clearSelection()

    if (nextReportType === REPORT_TYPES.master && knownFile) {
      await loadFile(knownFile)
      return
    }

    if (nextReportType === REPORT_TYPES.regression && regressionFiles.length > 0) {
      await loadFile(regressionFiles[0])
    }
  }

  async function changeSourceMode(nextMode) {
    setSourceMode(nextMode)
    setError('')
    clearSelection()
    setDashboardLoaded(false)
    setCacheByReport({ master: null, regression: null })
    setMasterFiles([])
    setRegressionFiles([])
    setKnownFile(null)
  }

  async function loadDirectSource() {
    if (!sourceUrl.trim()) {
      setError('Enter a Google Sheet or Drive folder URL.')
      return
    }

    setLoading(true)
    setError('')
    try {
      const response = await loadSourceUrl(sourceUrl.trim(), reportType, user)
      applyCacheResponse(response, reportType)
      setDashboardLoaded(true)
      setSourceMode(SOURCE_MODES.workspaceCloud)
    } catch (err) {
      setError(err.message || 'Unable to load source URL.')
    } finally {
      setLoading(false)
    }
  }

  async function downloadLocalSource() {
    if (!sourceUrl.trim()) {
      setError('Enter a Google Sheet or Drive folder ID or URL.')
      return
    }

    setLoading(true)
    setError('')
    try {
      const response = await downloadToLocal(sourceUrl.trim(), reportType, user)
      setSourceMode(SOURCE_MODES.local)
      applyCacheResponse(response, reportType)
      setDashboardLoaded(true)
    } catch (err) {
      setError(err.message || 'Unable to download source to local.')
    } finally {
      setLoading(false)
    }
  }

  async function changeFile(fileId) {
    const source = reportType === REPORT_TYPES.master ? [knownFile, ...masterFiles] : regressionFiles
    const file = source.find(item => item?.id === fileId)
    if (file) {
      loadCachedFile(file)
    }
  }

  function selectSheet(sheetName) {
    setSelectedSheet(sheetName)
    setActiveView(VIEW_TYPES.sheet)
    const cachedSheet = selectedFile?.sheets?.find(sheet => sheet.sheetName === sheetName)
    setSheetRows(cacheSheetToRowsResponse(selectedFileId, cachedSheet))
  }

  const rows = useMemo(() => sheetRows?.rows ?? [], [sheetRows])
  const analysisRows = useMemo(() => (allSheetRows.length > 0 ? allSheetRows : rows), [allSheetRows, rows])
  const filterOptions = useMemo(() => ({
    qaStatuses: uniqueValues(analysisRows, 'qaStatus'),
    devStatuses: uniqueValues(analysisRows, 'devStatus'),
    issueTypes: uniqueValues(analysisRows, 'issueType'),
    modules: uniqueValues(analysisRows, 'module'),
  }), [analysisRows])

  const applyFilters = useMemo(() => {
    return row => {
      const qaMatch = qaFilters.length === 0 || qaFilters.includes(row.qaStatus)
      const devMatch = devFilters.length === 0 || devFilters.includes(row.devStatus)
      const issueMatch = issueFilters.length === 0 || issueFilters.includes(row.issueType)
      const moduleMatch = moduleFilters.length === 0 || moduleFilters.includes(row.module)
      return rowMatchesSearch(row, search) && qaMatch && devMatch && issueMatch && moduleMatch
    }
  }, [search, qaFilters, devFilters, issueFilters, moduleFilters])

  const filteredRows = useMemo(() => {
    return rows.filter(applyFilters)
  }, [rows, applyFilters])

  const filteredAnalysisRows = useMemo(() => {
    return analysisRows.filter(applyFilters)
  }, [analysisRows, applyFilters])

  const moduleGroups = useMemo(() => {
    return groupEntries(filteredAnalysisRows, row => row.module).map(([module, moduleRows]) => ({
      module,
      rows: moduleRows,
      sheets: groupEntries(moduleRows, row => row.sheetName),
    }))
  }, [filteredAnalysisRows])

  const duplicateKeys = useMemo(() => {
    const counts = new Map()
    analysisRows.forEach(row => {
      const key = row.testCaseId || row.testCaseNo
      if (key) counts.set(key, (counts.get(key) ?? 0) + 1)
    })

    return new Set([...counts.entries()].filter(([, count]) => count > 1).map(([key]) => key))
  }, [analysisRows])

  const repeatedRows = useMemo(() => {
    return filteredAnalysisRows.filter(row => isRepeatedRow(row, duplicateKeys))
  }, [filteredAnalysisRows, duplicateKeys])

  const postponedRows = useMemo(() => {
    return filteredAnalysisRows.filter(isPostponedRow)
  }, [filteredAnalysisRows])

  const currentViewRows = useMemo(() => {
    if (activeView === VIEW_TYPES.repeated) return repeatedRows
    if (activeView === VIEW_TYPES.postponed) return postponedRows
    if (activeView === VIEW_TYPES.module) return filteredAnalysisRows
    return filteredRows
  }, [activeView, filteredRows, filteredAnalysisRows, repeatedRows, postponedRows])

  const selectedSummary = summary?.sheets?.find(sheet => sheet.sheetName === selectedSheet)
  const qaCounts = summary?.qaStatusCounts ?? {}
  const devCounts = summary?.devStatusCounts ?? {}
  const fileOptions = reportType === REPORT_TYPES.master
    ? [{ value: knownFile?.id ?? '', label: DEFAULT_FILE_NAME }].filter(option => option.value)
    : regressionFiles.map(file => ({ value: file.id, label: file.name }))

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Signed in as {user.displayName || user.username}</p>
          <h1>TestCaseViewer Dashboard</h1>
        </div>
        <div className="topbar-actions">
          <button className="secondary" type="button" onClick={() => loadDashboard()} disabled={loading}>
            Refresh
          </button>
          <button className="ghost" type="button" onClick={onLogout}>
            Logout
          </button>
        </div>
      </header>

      <section className="mode-tabs">
          <button
            type="button"
            className={sourceMode === SOURCE_MODES.workspaceCloud ? 'active' : ''}
            onClick={() => changeSourceMode(SOURCE_MODES.workspaceCloud)}
          >
          Workspace Cloud
        </button>
        <button
          type="button"
          className={sourceMode === SOURCE_MODES.local ? 'active' : ''}
          onClick={() => changeSourceMode(SOURCE_MODES.local)}
        >
          Local
        </button>
        <button
          type="button"
          className={sourceMode === SOURCE_MODES.hybrid ? 'active' : ''}
          onClick={() => changeSourceMode(SOURCE_MODES.hybrid)}
        >
          Hybrid
        </button>
        <button
          type="button"
          className={sourceMode === SOURCE_MODES.manual ? 'active' : ''}
          onClick={() => changeSourceMode(SOURCE_MODES.manual)}
        >
          Manual Upload
        </button>
        <button
          type="button"
          className={sourceMode === SOURCE_MODES.playwright ? 'active' : ''}
          onClick={() => changeSourceMode(SOURCE_MODES.playwright)}
        >
          Playwright Runs
        </button>
        <button
          type="button"
          className={sourceMode === SOURCE_MODES.startTesting ? 'active' : ''}
          onClick={() => changeSourceMode(SOURCE_MODES.startTesting)}
        >
          Start Testing
        </button>
      </section>

      {sourceMode === SOURCE_MODES.manual && (
        <ManualUploadPanel
          user={user}
          loading={loading}
          onSetLoading={setLoading}
          onError={setError}
          onCommitted={loadDashboard}
        />
      )}

      {sourceMode === SOURCE_MODES.playwright && (
        <PlaywrightRunsPanel
          user={user}
          loading={loading}
          onSetLoading={setLoading}
          onError={setError}
        />
      )}

      {sourceMode === SOURCE_MODES.startTesting && (
        <StartTestingPanel
          user={user}
          loading={loading}
          onSetLoading={setLoading}
          onError={setError}
        />
      )}

      {![SOURCE_MODES.manual, SOURCE_MODES.playwright, SOURCE_MODES.startTesting].includes(sourceMode) && !dashboardLoaded && !summary && !loading && !error && (
        <section className="empty-state">
          <h2>Connect the dashboard</h2>
          <p>
            {sourceMode === SOURCE_MODES.workspaceCloud && 'Load from Google Drive or Google Sheets.'}
            {sourceMode === SOURCE_MODES.local && 'View downloaded local TSV versions.'}
            {sourceMode === SOURCE_MODES.hybrid && 'Download from cloud to local, then work with local filters.'}
          </p>
          <button type="button" onClick={() => loadDashboard()}>Load dashboard</button>
        </section>
      )}

      {loading && <section className="notice">Loading data from ImpactSupport.Api...</section>}
      {error && <section className="notice error">{error}</section>}

      {![SOURCE_MODES.manual, SOURCE_MODES.playwright, SOURCE_MODES.startTesting].includes(sourceMode) && (dashboardLoaded || summary || knownFile || regressionFiles.length > 0) && (
        <>
          <section className="selector-panel">
            <div className="segmented">
              <button
                type="button"
                className={reportType === REPORT_TYPES.master ? 'active' : ''}
                onClick={() => changeReportType(REPORT_TYPES.master)}
              >
                Testcase_2026
              </button>
              <button
                type="button"
                className={reportType === REPORT_TYPES.regression ? 'active' : ''}
                onClick={() => changeReportType(REPORT_TYPES.regression)}
              >
                All Regression
              </button>
            </div>

            {sourceMode === SOURCE_MODES.workspaceCloud && (
              <button className="secondary" type="button" onClick={refreshCurrentIndex} disabled={loading}>
                Refresh index
              </button>
            )}

            {sourceMode === SOURCE_MODES.workspaceCloud && (
              <button className="secondary" type="button" onClick={syncCurrentChangedFiles} disabled={loading}>
                Sync changed files
              </button>
            )}

            {sourceMode !== SOURCE_MODES.local && (
              <label className="source-url-field">
                <span>{sourceMode === SOURCE_MODES.hybrid ? 'Sheet or folder ID / URL' : 'Source URL'}</span>
                <input
                  value={sourceUrl}
                  onChange={event => setSourceUrl(event.target.value)}
                  placeholder={sourceMode === SOURCE_MODES.hybrid ? 'Sheet or folder ID / URL' : 'Google Sheet or Drive folder URL'}
                />
              </label>
            )}

            {sourceMode === SOURCE_MODES.workspaceCloud && (
              <button className="secondary" type="button" onClick={loadDirectSource} disabled={loading || !sourceUrl.trim()}>
                Load URL
              </button>
            )}

            {sourceMode === SOURCE_MODES.hybrid && (
              <button className="secondary" type="button" onClick={downloadLocalSource} disabled={loading || !sourceUrl.trim()}>
                Download to Local
              </button>
            )}

            <SelectField
              label={reportType === REPORT_TYPES.master ? 'Master source' : 'Regression file'}
              value={selectedFileId}
              options={fileOptions}
              onChange={changeFile}
              placeholder="Select file"
            />

            <SelectField
              label="Sheet"
              value={selectedSheet}
              options={sheets.map(sheet => ({ value: sheet.name, label: sheet.name }))}
              onChange={selectSheet}
              placeholder="Select sheet"
            />

            {sourceMode === SOURCE_MODES.workspaceCloud && (
              <button className="secondary" type="button" onClick={refreshSelectedSheet} disabled={rowsLoading || !selectedSheet}>
                Refresh selected sheet
              </button>
            )}

            {sourceMode === SOURCE_MODES.workspaceCloud && (
              <button className="secondary" type="button" onClick={saveCurrentChanges} disabled={loading || !selectedFileId}>
                Save changes to Google
              </button>
            )}

            <button className="secondary" type="button" onClick={exportCurrentTsv} disabled={loading}>
              Export TSV
            </button>

            {reportType === REPORT_TYPES.regression && (
              <button className="secondary" type="button" onClick={analyzeSelectedRegressionFile} disabled={loading || !selectedFileId}>
                Analyze selected regression file
              </button>
            )}
          </section>

          <section className="source-grid">
            <article>
              <span>Selected source</span>
              <strong>{selectedFile?.name ?? DEFAULT_FILE_NAME}</strong>
              <small>
                {selectedFile?.syncStatus || selectedFile?.scanStatus || 'Local'}
                {selectedFile?.lastLocalSyncAt ? ` TSV ${new Date(selectedFile.lastLocalSyncAt).toLocaleString()}` : ''}
                {selectedFile?.lastMetadataSyncedAt ? ` metadata ${new Date(selectedFile.lastMetadataSyncedAt).toLocaleString()}` : ''}
                {selectedFile?.driveModifiedTime ? ` drive ${new Date(selectedFile.driveModifiedTime).toLocaleString()}` : ''}
                {selectedFile?.pendingEditCount ? ` pending ${selectedFile.pendingEditCount}` : ''}
              </small>
            </article>
            <article>
              <span>Testcase_2026</span>
              <strong>{knownFile ? 'Ready' : 'Not loaded'}</strong>
              <small>from KnownFileIds</small>
            </article>
            <article>
              <span>Regression files</span>
              <strong>{regressionFiles.length}</strong>
              <small>starts with Regression</small>
            </article>
          </section>

          {(selectedFile?.scanError || selectedFile?.syncError) && (
            <section className="notice error compact">{selectedFile.scanError || selectedFile.syncError}</section>
          )}

          {summary && (
            <>
              <section className="stats-grid">
                <StatCard label="Sheets" value={summary.totalSheets} hint="Google tabs" />
                <StatCard label="Test cases" value={summary.totalTestCases} hint="selected file" />
                <StatCard label="Selected rows" value={rows.length} hint="selected sheet" />
                <StatCard label="Filtered rows" value={filteredRows.length} hint="current view" />
                <StatCard label="Regression files" value={regressionFiles.length} hint="available" />
              </section>

              <section className="status-overview">
                <div>
                  <h2>QA Status</h2>
                  <div className="pill-row">
                    {Object.entries(qaCounts).map(([label, count]) => (
                      <StatusPill key={label} label={label} count={count} tone={statusTone(label)} />
                    ))}
                  </div>
                </div>
                <div>
                  <h2>Dev Status</h2>
                  <div className="pill-row">
                    {Object.entries(devCounts).map(([label, count]) => (
                      <StatusPill key={label} label={label} count={count} tone={statusTone(label)} />
                    ))}
                  </div>
                </div>
              </section>

              <section className="workbench">
                <aside className="sheet-list">
                  <div className="section-heading">
                    <h2>Sheets</h2>
                    <span>{sheets.length}</span>
                  </div>
                  {sheets.map(sheet => {
                    const itemSummary = summary.sheets.find(item => item.sheetName === sheet.name)
                    return (
                      <button
                        key={sheet.name}
                        className={sheet.name === selectedSheet ? 'sheet-button active' : 'sheet-button'}
                        type="button"
                        onClick={() => selectSheet(sheet.name)}
                      >
                        <strong>{sheet.name}</strong>
                        <span>{itemSummary?.module || 'Module pending'}</span>
                      </button>
                    )
                  })}
                </aside>

                <section className="sheet-detail">
                  <div className="section-heading">
                    <div>
                      <h2>
                        {activeView === VIEW_TYPES.sheet && selectedSheet}
                        {activeView === VIEW_TYPES.module && 'Module-wise'}
                        {activeView === VIEW_TYPES.repeated && 'Repeated Error/Modules'}
                        {activeView === VIEW_TYPES.postponed && 'Postponed/Future Development'}
                      </h2>
                      <p>{activeView === VIEW_TYPES.sheet ? selectedSummary?.module : `${currentViewRows.length} matching rows`}</p>
                    </div>
                    <span>{currentViewRows.length} / {activeView === VIEW_TYPES.sheet ? rows.length : analysisRows.length}</span>
                  </div>

                  <div className="view-tabs">
                    <button
                      type="button"
                      className={activeView === VIEW_TYPES.sheet ? 'active' : ''}
                      onClick={() => setActiveView(VIEW_TYPES.sheet)}
                    >
                      Sheet-wise
                    </button>
                    <button
                      type="button"
                      className={activeView === VIEW_TYPES.module ? 'active' : ''}
                      onClick={() => setActiveView(VIEW_TYPES.module)}
                    >
                      Module-wise
                    </button>
                    <button
                      type="button"
                      className={activeView === VIEW_TYPES.repeated ? 'active' : ''}
                      onClick={() => setActiveView(VIEW_TYPES.repeated)}
                    >
                      Repeated
                    </button>
                    <button
                      type="button"
                      className={activeView === VIEW_TYPES.postponed ? 'active' : ''}
                      onClick={() => setActiveView(VIEW_TYPES.postponed)}
                    >
                      Postponed/Future
                    </button>
                  </div>

                  <div className="search-row">
                    <label className="search-field">
                      <span>Search</span>
                      <input
                        value={search}
                        onChange={event => setSearch(event.target.value)}
                        placeholder="Search test cases, module, status, remarks"
                      />
                    </label>
                    <button className="secondary clear-button" type="button" onClick={clearFilters}>
                      Clear filters
                    </button>
                  </div>

                  <div className="filters">
                    <MultiSelect
                      label="QA Status"
                      options={filterOptions.qaStatuses}
                      selected={qaFilters}
                      onToggle={value => setQaFilters(toggleValue(qaFilters, value))}
                    />
                    <MultiSelect
                      label="Dev. Status"
                      options={filterOptions.devStatuses}
                      selected={devFilters}
                      onToggle={value => setDevFilters(toggleValue(devFilters, value))}
                    />
                    <MultiSelect
                      label="Issue Type"
                      options={filterOptions.issueTypes}
                      selected={issueFilters}
                      onToggle={value => setIssueFilters(toggleValue(issueFilters, value))}
                    />
                    <MultiSelect
                      label="Module/Sub Module"
                      options={filterOptions.modules}
                      selected={moduleFilters}
                      onToggle={value => setModuleFilters(toggleValue(moduleFilters, value))}
                    />
                  </div>

                  {rowsLoading && <div className="notice compact">Loading sheet rows...</div>}

                  {activeView === VIEW_TYPES.sheet && (
                    <RowTable
                      rows={filteredRows}
                      rowsLoading={rowsLoading}
                      emptyMessage="No rows match the selected search and filters."
                    />
                  )}

                  {activeView === VIEW_TYPES.module && (
                    <div className="group-stack">
                      {moduleGroups.map(group => (
                        <section className="result-group" key={group.module}>
                          <div className="group-heading">
                            <h3>{group.module}</h3>
                            <span>{group.rows.length} rows</span>
                          </div>
                          {group.sheets.map(([sheetName, sheetGroupRows]) => (
                            <div className="sheet-group" key={`${group.module}-${sheetName}`}>
                              <div className="sheet-group-heading">
                                <strong>{sheetName}</strong>
                                <span>{sheetGroupRows.length}</span>
                              </div>
                              <RowTable
                                rows={sheetGroupRows}
                                rowsLoading={rowsLoading}
                                emptyMessage="No rows found for this module and sheet."
                              />
                            </div>
                          ))}
                        </section>
                      ))}
                      {!rowsLoading && moduleGroups.length === 0 && (
                        <div className="notice compact">No module groups match the selected search and filters.</div>
                      )}
                    </div>
                  )}

                  {activeView === VIEW_TYPES.repeated && (
                    <RowTable
                      rows={repeatedRows}
                      rowsLoading={rowsLoading}
                      emptyMessage="No repeated errors or modules match the selected search and filters."
                    />
                  )}

                  {activeView === VIEW_TYPES.postponed && (
                    <RowTable
                      rows={postponedRows}
                      rowsLoading={rowsLoading}
                      emptyMessage="No postponed or future development rows match the selected search and filters."
                    />
                  )}
                </section>
              </section>
            </>
          )}
        </>
      )}
    </main>
  )
}

export default function App() {
  const storedUser = (() => {
    try {
      return JSON.parse(localStorage.getItem(SESSION_KEY) || 'null')
    } catch {
      return null
    }
  })()
  const [user, setUser] = useState(storedUser)

  function logout() {
    localStorage.removeItem(SESSION_KEY)
    setUser(null)
  }

  return user ? <DashboardPage user={user} onLogout={logout} /> : <LoginPage onLogin={setUser} />
}
