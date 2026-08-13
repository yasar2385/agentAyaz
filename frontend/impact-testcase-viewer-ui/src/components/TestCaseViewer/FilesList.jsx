import React, { useEffect, useState } from 'react'
import { getFiles } from '../../services/testCaseViewerApi'

function formatDate(iso) {
  if (!iso) return ''
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

export default function FilesList() {
  const [reportType, setReportType] = useState('master')
  const [files, setFiles] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    let mounted = true
    setLoading(true)
    setError(null)
    getFiles(reportType)
      .then(data => {
        if (!mounted) return
        setFiles(data)
      })
      .catch(err => {
        if (!mounted) return
        setError(err.message || 'Failed to load files')
      })
      .finally(() => mounted && setLoading(false))

    return () => (mounted = false)
  }, [reportType])

  return (
    <div>
      <div style={{ marginBottom: 12 }}>
        <label>
          Report type: 
          <select value={reportType} onChange={e => setReportType(e.target.value)}>
            <option value="master">Master</option>
            <option value="regression">Regression</option>
          </select>
        </label>
      </div>

      {loading && <div>Loading...</div>}
      {error && <div style={{ color: 'red' }}>Error: {error}</div>}

      {!loading && !error && files.length === 0 && <div>No files found.</div>}

      {!loading && !error && files.length > 0 && (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left' }}>Name</th>
              <th style={{ textAlign: 'left' }}>Modified</th>
            </tr>
          </thead>
          <tbody>
            {files.map(f => (
              <tr key={f.id}>
                <td>{f.name}</td>
                <td>{formatDate(f.modifiedTime)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
