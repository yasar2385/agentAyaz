import StatCard from './StatCard'

export default function ImportSummary({ batch }) {
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
