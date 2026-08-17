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
  runReportUrl,
} from '../../services/testCaseViewerApi'

export default function PlaywrightRunsPanel({ user, loading, onSetLoading, onError }) {
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
