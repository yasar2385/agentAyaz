export default function MultiSelect({ label, options, selected, onToggle }) {
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
