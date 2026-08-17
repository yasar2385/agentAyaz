import { useState } from 'react'
import { REPORT_TYPES } from '../../constants'
import StatusPill from '../common/StatusPill'
import ImportSummary from '../common/ImportSummary'
import {
  inspectImportFile,
  parseMasterImport,
  uploadResultImport,
  getImportErrors,
  saveMasterSheetActions,
  saveManualEditImportActions,
  commitImportBatch,
} from '../../services/testCaseViewerApi'

export default function ManualUploadPanel({ user, loading, onSetLoading, onError, onCommitted, uploadMode = 'both' }) {
  const [masterFile, setMasterFile] = useState(null)
  const [resultFiles, setResultFiles] = useState([])
  const [resultMode, setResultMode] = useState('single')
  const [masterBatch, setMasterBatch] = useState(null)
  const [resultBatch, setResultBatch] = useState(null)
  const [masterInspect, setMasterInspect] = useState(null)
  const [selectedMasterSheets, setSelectedMasterSheets] = useState([])
  const [errors, setErrors] = useState([])
  const [ackErrors, setAckErrors] = useState(false)
  const [message, setMessage] = useState('')

  async function runMasterUpload(event) {
    event.preventDefault()
    if (!masterFile) return
    await runImport(async () => {
      const inspect = await inspectImportFile(masterFile, user)
      const visibleSheets = (inspect.sheets ?? []).filter(sheet => sheet.visibility === 'visible').map(sheet => sheet.sheetName)
      setMasterInspect(inspect)
      setSelectedMasterSheets(visibleSheets)
      setMasterBatch(null)
      setResultBatch(null)
      setAckErrors(false)
      setErrors([])
      if (inspect.sourceType !== 'XLSX workbook') {
        const batch = await parseMasterImport(inspect.uploadToken, visibleSheets, user)
        setMasterBatch(batch)
        setMasterInspect(null)
        setSelectedMasterSheets([])
        setErrors(batch.errors ?? (batch.rowsError ? await getImportErrors(batch.batchId, user) : []))
        setMessage('Master upload dry-run is ready.')
      } else {
        setMessage('Choose workbook sheets before dry-run.')
      }
    })
  }

  async function parseSelectedMasterSheets() {
    if (!masterInspect) return
    await runImport(async () => {
      const batch = await parseMasterImport(masterInspect.uploadToken, selectedMasterSheets, user)
      setMasterBatch(batch)
      setMasterInspect(null)
      setSelectedMasterSheets([])
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

  async function updateManualEditAction(rowId, action) {
    if (!masterBatch) return
    const actions = (masterBatch.manualEditConflicts ?? []).map(conflict => ({
      rowId: conflict.rowId,
      action: conflict.rowId === rowId ? action : conflict.selectedAction,
    }))
    await runImport(async () => {
      const batch = await saveManualEditImportActions(masterBatch.batchId, actions, user)
      setMasterBatch(batch)
      setMessage('Manual edit conflict action saved.')
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

  function toggleMasterSheet(sheetName) {
    setSelectedMasterSheets(current => current.includes(sheetName)
      ? current.filter(item => item !== sheetName)
      : [...current, sheetName])
  }

  const activeBatch = masterBatch || resultBatch
  const hasErrors = (activeBatch?.rowsError ?? 0) > 0
  const unresolvedMasterConflicts = (masterBatch?.sheets ?? [])
    .filter(sheet => sheet.conflictStatus === 'EXISTS')
    .some(sheet => !['OVERWRITE', 'SKIP'].includes(sheet.selectedAction))
  const unresolvedManualEditConflicts = (masterBatch?.manualEditConflicts ?? [])
    .some(conflict => !['OVERWRITE', 'SKIP_ROW'].includes(conflict.selectedAction))
  const canCommitMaster = masterBatch && masterBatch.status !== 'COMMITTED' && !unresolvedMasterConflicts && !unresolvedManualEditConflicts && !hasErrors
  const canCommitResult = resultBatch && resultBatch.status !== 'COMMITTED' && !hasErrors
  const showMasterUpload = uploadMode === 'both' || uploadMode === 'master'
  const showResultUpload = uploadMode === 'both' || uploadMode === 'result'

  return (
    <section className="manual-upload-panel">
      <div className="upload-grid">
        {showMasterUpload && <form className="upload-card" onSubmit={runMasterUpload}>
          <div>
            <h2>Master Test Case Upload</h2>
            <p>CSV/TSV with Sheet Name, Module/Sub Module, and Test Case ID columns.</p>
          </div>
          <input type="file" accept=".xlsx,.csv,.tsv,text/csv,text/tab-separated-values" onChange={event => setMasterFile(event.target.files?.[0] ?? null)} />
          <button type="submit" disabled={loading || !masterFile}>Inspect master upload</button>
        </form>}

        {showResultUpload && <form className="upload-card" onSubmit={runResultUpload}>
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
        </form>}
      </div>

      {message && <section className="notice compact">{message}</section>}

      <ImportSummary batch={activeBatch} />

      {(activeBatch?.duplicateIdsResolved?.length ?? 0) > 0 && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Duplicate Test Case IDs</h2>
            <span>Informational</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Source row</th>
                  <th>Sheet/Page</th>
                  <th>Raw ID</th>
                  <th>Resolved ID</th>
                </tr>
              </thead>
              <tbody>
                {activeBatch.duplicateIdsResolved.map(item => (
                  <tr key={`${item.sheetName}-${item.sourceRowNumber}-${item.rawId}-${item.resolvedId}`}>
                    <td>{item.sourceRowNumber}</td>
                    <td>{item.sheetName}</td>
                    <td>{item.rawId}</td>
                    <td>{item.resolvedId}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {(masterBatch?.moduleClientPreview?.length ?? 0) > 0 && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Module Client Preview</h2>
            <span>Informational</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Source row</th>
                  <th>Raw module</th>
                  <th>Module</th>
                  <th>Clients</th>
                  <th>Sub-client</th>
                  <th>Type</th>
                  <th>DTD</th>
                </tr>
              </thead>
              <tbody>
                {masterBatch.moduleClientPreview.map(item => (
                  <tr key={`${item.sheetName}-${item.sourceRowNumber}-${item.rawModule}`}>
                    <td>{item.sourceRowNumber}</td>
                    <td>{item.rawModule}</td>
                    <td>{item.module}</td>
                    <td>{(item.clients ?? []).join(', ')}</td>
                    <td>{item.subClient}</td>
                    <td>{item.type}</td>
                    <td>{item.dtd}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {(masterBatch?.preconditionWildcardWarnings?.length ?? 0) > 0 && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Preconditions Wildcard Warnings</h2>
            <span>Non-blocking</span>
          </div>
          <div className="notice compact">These rows used unknown Preconditions text and will commit as all roles/all clients.</div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Source row</th>
                  <th>Sheet/Page</th>
                  <th>Raw Preconditions</th>
                </tr>
              </thead>
              <tbody>
                {masterBatch.preconditionWildcardWarnings.map(item => (
                  <tr key={`${item.sheetName}-${item.sourceRowNumber}-${item.rawValue}`}>
                    <td>{item.sourceRowNumber}</td>
                    <td>{item.sheetName}</td>
                    <td>{item.rawValue}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {masterInspect && masterInspect.sourceType === 'XLSX workbook' && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Workbook Sheet Selection</h2>
            <span>{masterInspect.sourceType}</span>
          </div>
          <div className="topbar-actions">
            <button className="secondary" type="button" onClick={() => setSelectedMasterSheets(masterInspect.sheets.filter(sheet => sheet.visibility === 'visible').map(sheet => sheet.sheetName))}>Select all visible</button>
            <button className="secondary" type="button" onClick={() => setSelectedMasterSheets(masterInspect.sheets.map(sheet => sheet.sheetName))}>Select all including hidden</button>
            <button className="ghost" type="button" onClick={() => setSelectedMasterSheets([])}>Clear</button>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Include</th>
                  <th>Sheet</th>
                  <th>Visibility</th>
                  <th>Rows</th>
                </tr>
              </thead>
              <tbody>
                {masterInspect.sheets.map(sheet => (
                  <tr key={sheet.sheetName}>
                    <td><input type="checkbox" checked={selectedMasterSheets.includes(sheet.sheetName)} onChange={() => toggleMasterSheet(sheet.sheetName)} /></td>
                    <td>{sheet.sheetName}</td>
                    <td>
                      <StatusPill
                        label={sheet.visibility === 'very_hidden' ? 'Hidden (advanced)' : sheet.visibility === 'hidden' ? 'Hidden in source file' : 'Visible'}
                        tone={sheet.visibility === 'visible' ? 'good' : sheet.visibility === 'very_hidden' ? 'bad' : 'warn'}
                      />
                    </td>
                    <td>{sheet.rowCountEstimate}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button type="button" onClick={parseSelectedMasterSheets} disabled={loading || selectedMasterSheets.length === 0}>
            Continue to dry-run
          </button>
        </section>
      )}

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

      {masterBatch && (masterBatch.manualEditConflicts?.length ?? 0) > 0 && (
        <section className="upload-review">
          <div className="section-heading">
            <h2>Manual Edit Conflicts</h2>
            <span>Row-level review</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Row</th>
                  <th>Sheet/Page</th>
                  <th>Test Case ID</th>
                  <th>Last edited</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {masterBatch.manualEditConflicts.map(conflict => (
                  <tr key={conflict.rowId}>
                    <td>{conflict.sourceRowNumber}</td>
                    <td>{conflict.sheetName}</td>
                    <td>{conflict.masterTestId}</td>
                    <td>{conflict.lastEditedBy || 'Manual edit'} {conflict.lastEditedAt ? new Date(conflict.lastEditedAt).toLocaleString() : ''}</td>
                    <td>
                      <select value={conflict.selectedAction} onChange={event => updateManualEditAction(conflict.rowId, event.target.value)} disabled={loading || masterBatch.status === 'COMMITTED'}>
                        <option value="">Choose</option>
                        <option value="OVERWRITE">Overwrite</option>
                        <option value="SKIP_ROW">Skip row</option>
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
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
