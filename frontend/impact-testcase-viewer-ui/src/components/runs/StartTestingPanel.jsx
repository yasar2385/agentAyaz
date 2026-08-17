import { useState } from 'react'
import { isRunManager } from '../../utils/runHelpers'
import StatusPill from '../common/StatusPill'
import SelectField from '../common/SelectField'
import {
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
} from '../../services/testCaseViewerApi'

export default function StartTestingPanel({ user, loading, onSetLoading, onError }) {
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

  async function triggerConfig(configId) {
    await runAction(async () => {
      const started = await triggerRunConfig(configId, user)
      setExecution(started)
      setMessage(`Execution ${started.id} queued.`)
      await loadStartTesting(recentScope)
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
  const terminal = ['PASSED', 'FAILED', 'ERROR', 'CANCELLED'].includes(execution?.status)

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
              <tr><th>Config</th><th>Last module</th><th>Next module</th><th>Actions</th></tr>
            </thead>
            <tbody>
              {configs.map(config => {
                const progress = progressByConfig[config.id] ?? {}
                return (
                  <tr key={config.id}>
                    <td>{config.testingName}</td>
                    <td>{progress.lastModuleName || 'Not started'}</td>
                    <td>{progress.nextModuleName || 'Complete'}</td>
                    <td>
                      <div className="topbar-actions">
                        <button type="button" disabled={loading || !canManage || blockers.length > 0} onClick={() => triggerConfig(config.id)}>Run</button>
                        <button type="button" className="secondary" disabled={loading || !canManage || blockers.length > 0 || !progress.nextModuleName} onClick={() => continueConfig(config.id)}>Continue</button>
                      </div>
                    </td>
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
          <div className="topbar-actions">
            {!terminal && <button className="secondary" type="button" onClick={pollExecution} disabled={loading}>Refresh status</button>}
            {!terminal && canManage && <button type="button" onClick={cancelExecution} disabled={loading}>Cancel</button>}
            {reportStatuses.includes(execution.status) && <a className="button-link" href={runReportUrl(execution.id)} target="_blank" rel="noreferrer">Open report</a>}
          </div>
        </section>
      )}

      {message && <section className="notice compact">{message}</section>}
    </section>
  )
}
