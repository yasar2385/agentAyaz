export function ReviewTextField({ label, value, onChange }) {
  return <label className="select-field"><span>{label}</span><input value={value ?? ''} onChange={event => onChange(event.target.value)} /></label>
}

export function ReviewTextAreaField({ label, value, onChange }) {
  return <label className="select-field"><span>{label}</span><textarea value={value ?? ''} onChange={event => onChange(event.target.value)} /></label>
}

export function ReviewSelectField({ label, value, options = [], onChange, allowBlank = false }) {
  return (
    <label className="select-field">
      <span>{label}</span>
      <select value={value ?? ''} onChange={event => onChange(event.target.value ? Number(event.target.value) : null)}>
        {allowBlank && <option value="">Blank</option>}
        {options.map(option => <option key={option.id} value={option.id}>{option.value}</option>)}
      </select>
    </label>
  )
}

export function ReviewMultiSelectField({ label, value = [], options = [], onChange }) {
  return (
    <label className="select-field">
      <span>{label}</span>
      <select multiple value={value.map(String)} onChange={event => onChange([...event.target.selectedOptions].map(option => Number(option.value)))}>
        {options.map(option => <option key={option.id} value={option.id}>{option.value}</option>)}
      </select>
    </label>
  )
}
