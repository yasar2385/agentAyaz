import { useState, useEffect } from 'react'
import { statusTone } from '../../utils/dashboardHelpers'
import { blankReviewDetail, toReviewForm } from '../../utils/reviewHelpers'
import StatusPill from '../common/StatusPill'
import { ReviewTextField, ReviewTextAreaField, ReviewSelectField, ReviewMultiSelectField } from './ReviewFields'
import {
  getMasterReviewModules,
  getMasterReviewList,
  getMasterReviewLookups,
  getMasterReviewDetail,
  createMasterReviewDetail,
  updateMasterReviewDetail,
  deleteMasterReviewDetail,
} from '../../services/testCaseViewerApi'

function emptyReviewFilters() {
  return { moduleId: '', clientId: '', roleId: '', round: '', search: '' }
}

function readReviewFiltersFromUrl() {
  const params = new URLSearchParams(window.location.search)
  return {
    moduleId: params.get('moduleId') ?? '',
    clientId: params.get('clientId') ?? '',
    roleId: params.get('roleId') ?? '',
    round: params.get('round') ?? '',
    search: params.get('search') ?? '',
  }
}

function cleanReviewFilters(filters) {
  return Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== undefined && value !== null && value !== ''))
}

function writeReviewFiltersToUrl(filters) {
  const params = new URLSearchParams(window.location.search)
  for (const key of ['moduleId', 'clientId', 'roleId', 'round', 'search']) {
    const value = filters[key]
    if (value === undefined || value === null || value === '') {
      params.delete(key)
    } else {
      params.set(key, String(value))
    }
  }
  const query = params.toString()
  window.history.replaceState(null, '', `${window.location.pathname}${query ? `?${query}` : ''}${window.location.hash}`)
}

export default function ReviewEditPanel({ user, loading, onSetLoading, onError }) {
  const [modules, setModules] = useState([])
  const [lookups, setLookups] = useState(null)
  const [filters, setFilters] = useState(readReviewFiltersFromUrl)
  const [list, setList] = useState({ items: [], page: 1, pageSize: 25, totalCount: 0 })
  const [detail, setDetail] = useState(null)
  const [form, setForm] = useState(null)
  const [message, setMessage] = useState('')
  const isCreate = detail?.isNew

  useEffect(() => {
    loadInitial()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function run(action) {
    onSetLoading(true)
    onError('')
    setMessage('')
    try {
      await action()
    } catch (err) {
      onError(err.message || 'Review & Edit action failed.')
    } finally {
      onSetLoading(false)
    }
  }

  async function loadInitial() {
    await run(async () => {
      const [moduleRows, lookupRows] = await Promise.all([getMasterReviewModules(user), getMasterReviewLookups(user)])
      setModules(moduleRows)
      setLookups(lookupRows)
      await loadList(filters, 1)
    })
  }

  async function loadList(nextFilters = filters, page = 1) {
    const rows = await getMasterReviewList(cleanReviewFilters(nextFilters), page, 25, user)
    setList(rows)
    return rows
  }

  async function chooseModule(moduleId) {
    await updateFilters({ moduleId })
  }

  async function updateFilters(patch) {
    const nextFilters = { ...filters, ...patch }
    setFilters(nextFilters)
    writeReviewFiltersToUrl(nextFilters)
    await run(async () => {
      setDetail(null)
      setForm(null)
      await loadList(nextFilters, 1)
    })
  }

  async function clearFilters() {
    const nextFilters = emptyReviewFilters()
    setFilters(nextFilters)
    writeReviewFiltersToUrl(nextFilters)
    await run(async () => {
      setDetail(null)
      setForm(null)
      await loadList(nextFilters, 1)
    })
  }

  async function openDetail(masterTestId) {
    await run(async () => {
      const row = await getMasterReviewDetail(masterTestId, user)
      setDetail(row)
      setForm(toReviewForm(row))
    })
  }

  function createNew() {
    const row = blankReviewDetail(filters.moduleId)
    setDetail(row)
    setForm(toReviewForm(row))
    setMessage('')
  }

  async function saveDetail(event) {
    event.preventDefault()
    if (!detail || !form) return
    await run(async () => {
      const payload = { ...form, lastKnownUpdatedAt: detail.masterUpdatedAt }
      const saved = isCreate
        ? await createMasterReviewDetail(payload, user)
        : await updateMasterReviewDetail(detail.masterTestId, payload, user)
      setDetail(saved)
      setForm(toReviewForm(saved))
      const nextFilters = { ...filters, moduleId: saved.moduleId ?? filters.moduleId }
      setFilters(nextFilters)
      writeReviewFiltersToUrl(nextFilters)
      await loadList(nextFilters, 1)
      setMessage(isCreate ? 'Test case created.' : 'Test case saved.')
    })
  }

  async function deleteDetail() {
    if (!detail || isCreate) return
    if (!window.confirm(`Delete ${detail.masterTestId}? This will soft-delete the test case.`)) return
    await run(async () => {
      await deleteMasterReviewDetail(detail.masterTestId, user)
      setDetail(null)
      setForm(null)
      await loadList(filters, list.page)
      setMessage('Test case soft-deleted.')
    })
  }

  function setField(field, value) {
    setForm(current => ({ ...current, [field]: value }))
  }

  function setRemark(roundNumber, field, value) {
    setForm(current => ({
      ...current,
      remarks: current.remarks.map(remark => remark.roundNumber === roundNumber ? { ...remark, [field]: value } : remark),
    }))
  }

  const totalPages = Math.max(1, Math.ceil((list.totalCount ?? 0) / (list.pageSize ?? 25)))

  return (
    <section className="review-edit-panel">
      {message && <section className="notice compact">{message}</section>}
      <div className="review-layout">
        <aside className="upload-review">
          <div className="section-heading">
            <h2>Modules</h2>
            <button type="button" className="secondary" onClick={createNew}>Create</button>
          </div>
          <div className="module-list">
            {modules.map(module => (
              <button key={module.moduleId} type="button" className={Number(filters.moduleId) === module.moduleId ? 'active' : 'secondary'} onClick={() => chooseModule(module.moduleId)}>
                <span>{module.moduleName}</span>
                <strong>{module.testCaseCount}</strong>
              </button>
            ))}
          </div>
        </aside>
        <section className="upload-review">
          <div className="review-filter-bar">
            <label className="select-field">
              <span>Module</span>
              <select value={filters.moduleId} onChange={event => updateFilters({ moduleId: event.target.value })}>
                <option value="">All modules</option>
                {modules.map(module => <option key={module.moduleId} value={module.moduleId}>{module.moduleName}</option>)}
              </select>
            </label>
            <label className="select-field">
              <span>Name</span>
              <input value={filters.search} onChange={event => updateFilters({ search: event.target.value })} placeholder="Test case ID or description" />
            </label>
            <label className="select-field">
              <span>Client</span>
              <select value={filters.clientId} onChange={event => updateFilters({ clientId: event.target.value })}>
                <option value="">All clients</option>
                {(lookups?.clients ?? []).map(client => <option key={client.id} value={client.id}>{client.value}</option>)}
              </select>
            </label>
            <label className="select-field">
              <span>Role</span>
              <select value={filters.roleId} onChange={event => updateFilters({ roleId: event.target.value })}>
                <option value="">All roles</option>
                {(lookups?.preconditionRoles ?? []).filter(role => role.value !== 'PE').map(role => <option key={role.id} value={role.id}>{role.value}</option>)}
              </select>
            </label>
            <label className="select-field">
              <span>Round</span>
              <select value={filters.round} onChange={event => updateFilters({ round: event.target.value })}>
                <option value="">All rounds</option>
                <option value="1">1st</option>
                <option value="2">2nd</option>
                <option value="3">3rd</option>
                <option value="4">4th</option>
              </select>
            </label>
            <button type="button" className="secondary" onClick={clearFilters} disabled={loading}>Clear filters</button>
          </div>
          <div className="section-heading">
            <h2>Test Cases</h2>
            <span>{list.totalCount ?? 0}</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Test Case ID</th><th>No.</th><th>QA</th><th>Dev</th><th>Updated</th></tr></thead>
              <tbody>
                {(list.items ?? []).map(row => (
                  <tr key={row.masterTestId} onClick={() => openDetail(row.masterTestId)}>
                    <td>{row.masterTestId}</td>
                    <td>{row.masterTestNo}</td>
                    <td><StatusPill label={row.qaStatus || 'Blank'} tone={statusTone(row.qaStatus)} /></td>
                    <td><StatusPill label={row.devStatus || 'Blank'} tone={statusTone(row.devStatus)} /></td>
                    <td>{row.masterUpdatedAt ? new Date(row.masterUpdatedAt).toLocaleString() : ''}</td>
                  </tr>
                ))}
                {(list.items ?? []).length === 0 && <tr><td colSpan="5" className="empty-cell">No active master test cases.</td></tr>}
              </tbody>
            </table>
          </div>
          <div className="topbar-actions">
            <button type="button" className="secondary" disabled={loading || list.page <= 1} onClick={() => run(() => loadList(filters, list.page - 1))}>Previous</button>
            <span>{list.page} / {totalPages}</span>
            <button type="button" className="secondary" disabled={loading || list.page >= totalPages} onClick={() => run(() => loadList(filters, list.page + 1))}>Next</button>
          </div>
        </section>
        <section className="upload-review detail-editor">
          <div className="section-heading">
            <h2>{isCreate ? 'Create Test Case' : 'Detail'}</h2>
            <span>{detail?.masterTestId || 'Select a row'}</span>
          </div>
          {detail && form && lookups ? (
            <form className="review-form" onSubmit={saveDetail}>
              {isCreate && <ReviewTextField label="Test case ID" value={form.masterTestId} onChange={value => setField('masterTestId', value)} />}
              <div className="form-grid">
                <ReviewTextField label="Test no." value={form.masterTestNo} onChange={value => setField('masterTestNo', value)} />
                <ReviewSelectField label="Module" value={form.moduleId} options={lookups.modules} onChange={value => setField('moduleId', value)} />
                <ReviewSelectField label="Precondition role" value={form.preconditionRoleId} options={lookups.preconditionRoles} onChange={value => setField('preconditionRoleId', value)} allowBlank />
                <ReviewSelectField label="Type" value={form.masterTypeId} options={lookups.contentTypes} onChange={value => setField('masterTypeId', value)} allowBlank />
                <ReviewSelectField label="Issue type" value={form.issueTypeId} options={lookups.issueTypes} onChange={value => setField('issueTypeId', value)} allowBlank />
                <ReviewSelectField label="QA status" value={form.qaStatusId} options={lookups.qaStatuses} onChange={value => setField('qaStatusId', value)} allowBlank />
                <ReviewSelectField label="Dev status" value={form.devStatusId} options={lookups.devStatuses} onChange={value => setField('devStatusId', value)} allowBlank />
              </div>
              <div className="form-grid two">
                <ReviewMultiSelectField label="Testing types" value={form.testingTypeIds} options={lookups.testingTypes} onChange={value => setField('testingTypeIds', value)} />
                <ReviewMultiSelectField label="Clients" value={form.clientIds} options={lookups.clients} onChange={value => setField('clientIds', value)} />
              </div>
              <ReviewTextAreaField label="Description" value={form.masterDescription} onChange={value => setField('masterDescription', value)} />
              <ReviewTextAreaField label="Test steps" value={form.masterTestSteps} onChange={value => setField('masterTestSteps', value)} />
              <ReviewTextAreaField label="Test data" value={form.masterTestData} onChange={value => setField('masterTestData', value)} />
              <ReviewTextAreaField label="Expected result" value={form.masterExpectedResult} onChange={value => setField('masterExpectedResult', value)} />
              <ReviewTextAreaField label="Actual result" value={form.masterActualResult} onChange={value => setField('masterActualResult', value)} />
              <div className="remark-grid">
                {form.remarks.map(remark => (
                  <div key={remark.roundNumber} className="remark-block">
                    <h3>Round {remark.roundNumber}</h3>
                    <ReviewTextAreaField label="QA remarks" value={remark.qaRemark} onChange={value => setRemark(remark.roundNumber, 'qaRemark', value)} />
                    <ReviewTextAreaField label="Dev remarks" value={remark.devRemark} onChange={value => setRemark(remark.roundNumber, 'devRemark', value)} />
                  </div>
                ))}
              </div>
              <div className="topbar-actions">
                <button type="submit" disabled={loading || (isCreate && !form.masterTestId.trim())}>{isCreate ? 'Create' : 'Save changes'}</button>
                {!isCreate && <button type="button" className="secondary" disabled={loading} onClick={() => openDetail(detail.masterTestId)}>Reload</button>}
                {!isCreate && <button type="button" className="ghost" disabled={loading} onClick={deleteDetail}>Delete</button>}
              </div>
              <details>
                <summary>Edit history</summary>
                <div className="history-list">
                  {(detail.editHistory ?? []).map(item => <p key={item.id}><strong>{item.fieldName}</strong> changed by {item.editedBy} on {new Date(item.editedAt).toLocaleString()}</p>)}
                  {(detail.editHistory ?? []).length === 0 && <p>No manual edits yet.</p>}
                </div>
              </details>
            </form>
          ) : <p>Select a test case to review and edit committed master data.</p>}
        </section>
      </div>
    </section>
  )
}
