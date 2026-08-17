import { statusTone, latestRound, roundLabel } from '../../utils/dashboardHelpers'
import StatusPill from './StatusPill'

export default function RowTable({ rows, rowsLoading, emptyMessage }) {
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
