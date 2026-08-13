import React, { useMemo, useState } from 'react'
import './App.css'
import {
  getDashboardSummary,
  getFiles,
  getKnownFile,
  getSheetRows,
  getSheets,
} from './services/testCaseViewerApi'

const SESSION_KEY = 'impact-testcase-viewer-session'
const DEFAULT_FILE_NAME = 'Testcase_2026'

function LoginPage({ onLogin }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  function submit(event) {
    event.preventDefault()
    if (!username.trim() || !password.trim()) {
      setError('Enter username and password.')
      return
    }

    localStorage.setItem(SESSION_KEY, JSON.stringify({ username: username.trim() }))
    onLogin(username.trim())
  }

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div>
          <p className="eyebrow">Impact QA</p>
          <h1>TestCaseViewer</h1>
          <p className="login-copy">Review master and regression coverage from the configured .NET backend.</p>
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
          <button type="submit">Sign in</button>
        </form>
      </section>
    </main>
  )
}

function StatusPill({ label, count, tone = 'neutral' }) {
  return (
    <span className={`status-pill ${tone}`}>
      <span>{label}</span>
      <strong>{count}</strong>
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
        {options.length === 0 && <small>No statuses found</small>}
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

function statusTone(status) {
  const value = status.toLowerCase()
  if (value.includes('pass') || value.includes('closed') || value.includes('fixed')) return 'good'
  if (value.includes('fail') || value.includes('reject') || value.includes('reopen')) return 'bad'
  if (value.includes('wip') || value.includes('clear')) return 'warn'
  return 'neutral'
}

function toggleValue(values, value) {
  return values.includes(value) ? values.filter(item => item !== value) : [...values, value]
}

function DashboardPage({ user, onLogout }) {
  const [masterFile, setMasterFile] = useState(null)
  const [masterFiles, setMasterFiles] = useState([])
  const [regressionFiles, setRegressionFiles] = useState([])
  const [sheets, setSheets] = useState([])
  const [summary, setSummary] = useState(null)
  const [selectedSheet, setSelectedSheet] = useState('')
  const [sheetRows, setSheetRows] = useState(null)
  const [qaFilters, setQaFilters] = useState([])
  const [devFilters, setDevFilters] = useState([])
  const [loading, setLoading] = useState(false)
  const [rowsLoading, setRowsLoading] = useState(false)
  const [error, setError] = useState('')

  async function loadDashboard() {
    setLoading(true)
    setError('')
    setQaFilters([])
    setDevFilters([])

    try {
      const [known, masters, regressions] = await Promise.all([
        getKnownFile(DEFAULT_FILE_NAME),
        getFiles('master'),
        getFiles('regression'),
      ])

      setMasterFile(known)
      setMasterFiles(masters)
      setRegressionFiles(regressions)

      const [sheetList, dashboardSummary] = await Promise.all([
        getSheets(known.id),
        getDashboardSummary(known.id),
      ])

      setSheets(sheetList)
      setSummary(dashboardSummary)
      const firstSheet = sheetList[0]?.name ?? ''
      setSelectedSheet(firstSheet)
      if (firstSheet) {
        await loadRows(known.id, firstSheet)
      }
    } catch (err) {
      setError(err.message || 'Unable to load dashboard.')
    } finally {
      setLoading(false)
    }
  }

  async function loadRows(fileId, sheetName) {
    setRowsLoading(true)
    setQaFilters([])
    setDevFilters([])
    try {
      const response = await getSheetRows(fileId, sheetName)
      setSheetRows(response)
    } catch (err) {
      setError(err.message || 'Unable to load sheet rows.')
    } finally {
      setRowsLoading(false)
    }
  }

  function selectSheet(sheetName) {
    setSelectedSheet(sheetName)
    if (masterFile?.id) {
      loadRows(masterFile.id, sheetName)
    }
  }

  const filteredRows = useMemo(() => {
    const rows = sheetRows?.rows ?? []
    return rows.filter(row => {
      const qaMatch = qaFilters.length === 0 || qaFilters.includes(row.qaStatus)
      const devMatch = devFilters.length === 0 || devFilters.includes(row.devStatus)
      return qaMatch && devMatch
    })
  }, [sheetRows, qaFilters, devFilters])

  const selectedSummary = summary?.sheets?.find(sheet => sheet.sheetName === selectedSheet)
  const qaCounts = summary?.qaStatusCounts ?? {}
  const devCounts = summary?.devStatusCounts ?? {}

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Signed in as {user}</p>
          <h1>TestCaseViewer Dashboard</h1>
        </div>
        <div className="topbar-actions">
          <button className="secondary" type="button" onClick={loadDashboard} disabled={loading}>
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
          <p>Load `Testcase_2026`, master files, and regression files from the configured .NET API.</p>
          <button type="button" onClick={loadDashboard}>Load dashboard</button>
        </section>
      )}

      {loading && <section className="notice">Loading data from ImpactSupport.Api...</section>}
      {error && <section className="notice error">{error}</section>}

      {summary && (
        <>
          <section className="source-grid">
            <article>
              <span>Known master source</span>
              <strong>{masterFile?.name ?? DEFAULT_FILE_NAME}</strong>
              <small>{masterFile?.id}</small>
            </article>
            <article>
              <span>Master files</span>
              <strong>{masterFiles.length}</strong>
              <small>from MasterFolderId</small>
            </article>
            <article>
              <span>Regression files</span>
              <strong>{regressionFiles.length}</strong>
              <small>starts with Regression</small>
            </article>
          </section>

          <section className="stats-grid">
            <StatCard label="Sheets" value={summary.totalSheets} hint="Google tabs" />
            <StatCard label="Test cases" value={summary.totalTestCases} hint="parsed from row 24+" />
            <StatCard label="Passed" value={qaCounts.Passed ?? qaCounts.passed ?? 0} hint="QA status" />
            <StatCard label="Failed" value={qaCounts.Failed ?? qaCounts.failed ?? 0} hint="QA status" />
            <StatCard label="Closed" value={qaCounts.Closed ?? qaCounts.closed ?? 0} hint="QA status" />
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
                <span>{filteredRows.length} / {sheetRows?.rows?.length ?? 0}</span>
              </div>

              <div className="filters">
                <MultiSelect
                  label="QA Status"
                  options={sheetRows?.qaStatuses ?? []}
                  selected={qaFilters}
                  onToggle={value => setQaFilters(toggleValue(qaFilters, value))}
                />
                <MultiSelect
                  label="Dev. Status"
                  options={sheetRows?.devStatuses ?? []}
                  selected={devFilters}
                  onToggle={value => setDevFilters(toggleValue(devFilters, value))}
                />
                <button className="secondary clear-button" type="button" onClick={() => { setQaFilters([]); setDevFilters([]) }}>
                  Clear filters
                </button>
              </div>

              {rowsLoading && <div className="notice compact">Loading sheet rows...</div>}

              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>TC ID</th>
                      <th>Module</th>
                      <th>QA Status</th>
                      <th>Dev. Status</th>
                      <th>Issue Type</th>
                      <th>Actual Result</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredRows.map((row, index) => (
                      <tr key={`${row.testCaseId}-${index}`}>
                        <td>{row.testCaseId || row.testCaseNo}</td>
                        <td>{row.module}</td>
                        <td><StatusPill label={row.qaStatus || 'Blank'} count="" tone={statusTone(row.qaStatus || '')} /></td>
                        <td><StatusPill label={row.devStatus || 'Blank'} count="" tone={statusTone(row.devStatus || '')} /></td>
                        <td>{row.issueType}</td>
                        <td>{row.actualResult}</td>
                      </tr>
                    ))}
                    {!rowsLoading && filteredRows.length === 0 && (
                      <tr>
                        <td colSpan="6" className="empty-cell">No rows match the selected filters.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </section>
        </>
      )}
    </main>
  )
}

export default function App() {
  const storedUser = (() => {
    try {
      return JSON.parse(localStorage.getItem(SESSION_KEY) || 'null')?.username ?? ''
    } catch {
      return ''
    }
  })()
  const [user, setUser] = useState(storedUser)

  function logout() {
    localStorage.removeItem(SESSION_KEY)
    setUser('')
  }

  return user ? <DashboardPage user={user} onLogout={logout} /> : <LoginPage onLogin={setUser} />
}
