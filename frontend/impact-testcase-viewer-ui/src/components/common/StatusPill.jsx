export default function StatusPill({ label, count, tone = 'neutral' }) {
  return (
    <span className={`status-pill ${tone}`}>
      <span>{label}</span>
      {count !== undefined && count !== '' && <strong>{count}</strong>}
    </span>
  )
}
