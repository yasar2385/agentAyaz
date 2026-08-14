import { useMemo, useState } from 'react'
import './App.css'
import {
  getDashboardSummary,
  getFiles,
  getKnownFile,
  getSheetRows,
  getSheets,
  login,
} from './services/testCaseViewerApi'

const SESSION_KEY = 'impact-testcase-viewer-session'
const DEFAULT_FILE_NAME = 'Testcase_2026'
const REPORT_TYPES = {
  master: 'master',
  regression: 'regression',
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
    ...(row.qaRemarks ?? []),
    ...(row.devRemarks ?? []),
  ]

  return searchable.some(item => String(item ?? '').toLowerCase().includes(value))
}

function DashboardPage({ user, onLogout }) {
  const [reportType, setReportType] = useState(REPORT_TYPES.master)
  const [knownFile, setKnownFile] = useState(null)
  const [masterFiles, setMasterFiles] = useState([])
  const [regressionFiles, setRegressionFiles] = useState([])
  const [selectedFileId, setSelectedFileId] = useState('')
  const [selectedFile, setSelectedFile] = useState(null)
  const [sheets, setSheets] = useState([])
  const [summary, setSummary] = useState(null)
  const [selectedSheet, setSelectedSheet] = useState('')
  const [sheetRows, setSheetRows] = useState(null)
  const [search, setSearch] = useState('')
  const [qaFilters, setQaFilters] = useState([])
  const [devFilters, setDevFilters] = useState([])
  const [issueFilters, setIssueFilters] = useState([])
  const [moduleFilters, setModuleFilters] = useState([])
  const [loading, setLoading] = useState(false)
  const [rowsLoading, setRowsLoading] = useState(false)
  const [error, setError] = useState('')

  async function loadDashboard(nextReportType = reportType) {
    setLoading(true)
    setError('')
    clearSelection()

    try {
      const [known, masters, regressions] = await Promise.all([
        getKnownFile(DEFAULT_FILE_NAME),
        getFiles('master'),
        getFiles('regression'),
      ])

      setKnownFile(known)
      setMasterFiles(masters)
      setRegressionFiles(regressions)

      if (nextReportType === REPORT_TYPES.master) {
        await loadFile(known)
      } else {
        const firstRegression = regressions[0] ?? null
        if (firstRegression) {
          await loadFile(firstRegression)
        }
      }
    } catch (err) {
      setError(err.message || 'Unable to load dashboard.')
    } finally {
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
    if (!file?.id) return

    setSelectedFile(file)
    setSelectedFileId(file.id)
    setSheets([])
    setSummary(null)
    setSelectedSheet('')
    setSheetRows(null)
    clearFilters()

    const [sheetList, dashboardSummary] = await Promise.all([
      getSheets(file.id),
      getDashboardSummary(file.id),
    ])

    setSheets(sheetList)
    setSummary(dashboardSummary)
    const firstSheet = sheetList[0]?.name ?? ''
    setSelectedSheet(firstSheet)
    if (firstSheet) {
      await loadRows(file.id, firstSheet)
    }
  }

  async function loadRows(fileId, sheetName) {
    setRowsLoading(true)
    setError('')
    clearFilters()

    try {
      const response = await getSheetRows(fileId, sheetName)
      setSheetRows(response)
    } catch (err) {
      setError(err.message || 'Unable to load sheet rows.')
    } finally {
      setRowsLoading(false)
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

  async function changeFile(fileId) {
    const source = reportType === REPORT_TYPES.master ? [knownFile, ...masterFiles] : regressionFiles
    const file = source.find(item => item?.id === fileId)
    if (file) {
      await loadFile(file)
    }
  }

  function selectSheet(sheetName) {
    setSelectedSheet(sheetName)
    if (selectedFileId) {
      loadRows(selectedFileId, sheetName)
    }
  }

  const rows = useMemo(() => sheetRows?.rows ?? [], [sheetRows])
  const filterOptions = useMemo(() => ({
    qaStatuses: uniqueValues(rows, 'qaStatus'),
    devStatuses: uniqueValues(rows, 'devStatus'),
    issueTypes: uniqueValues(rows, 'issueType'),
    modules: uniqueValues(rows, 'module'),
  }), [rows])

  const filteredRows = useMemo(() => {
    return rows.filter(row => {
      const qaMatch = qaFilters.length === 0 || qaFilters.includes(row.qaStatus)
      const devMatch = devFilters.length === 0 || devFilters.includes(row.devStatus)
      const issueMatch = issueFilters.length === 0 || issueFilters.includes(row.issueType)
      const moduleMatch = moduleFilters.length === 0 || moduleFilters.includes(row.module)
      return rowMatchesSearch(row, search) && qaMatch && devMatch && issueMatch && moduleMatch
    })
  }, [rows, search, qaFilters, devFilters, issueFilters, moduleFilters])

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

      {!summary && !loading && !error && (
        <section className="empty-state">
          <h2>Connect the dashboard</h2>
          <p>Load `Testcase_2026` and all regression sheets from the configured .NET API.</p>
          <button type="button" onClick={() => loadDashboard()}>Load dashboard</button>
        </section>
      )}

      {loading && <section className="notice">Loading data from ImpactSupport.Api...</section>}
      {error && <section className="notice error">{error}</section>}

      {(summary || knownFile || regressionFiles.length > 0) && (
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
          </section>

          <section className="source-grid">
            <article>
              <span>Selected source</span>
              <strong>{selectedFile?.name ?? DEFAULT_FILE_NAME}</strong>
              <small>{selectedFileId || knownFile?.id}</small>
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
                      <h2>{selectedSheet}</h2>
                      <p>{selectedSummary?.module}</p>
                    </div>
                    <span>{filteredRows.length} / {rows.length}</span>
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

                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr>
                          <th>TC No.</th>
                          <th>TC ID</th>
                          <th>Module</th>
                          <th>Description</th>
                          <th>QA Status</th>
                          <th>Dev. Status</th>
                          <th>Issue Type</th>
                          <th>Actual Result</th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredRows.map((row, index) => (
                          <tr key={`${row.testCaseId}-${row.testCaseNo}-${index}`}>
                            <td>{row.testCaseNo}</td>
                            <td>{row.testCaseId}</td>
                            <td>{row.module}</td>
                            <td>{row.description}</td>
                            <td><StatusPill label={row.qaStatus || 'Blank'} tone={statusTone(row.qaStatus || '')} /></td>
                            <td><StatusPill label={row.devStatus || 'Blank'} tone={statusTone(row.devStatus || '')} /></td>
                            <td>{row.issueType}</td>
                            <td>{row.actualResult}</td>
                          </tr>
                        ))}
                        {!rowsLoading && filteredRows.length === 0 && (
                          <tr>
                            <td colSpan="8" className="empty-cell">No rows match the selected search and filters.</td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>
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
