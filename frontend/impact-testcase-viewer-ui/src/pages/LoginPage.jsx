import { useState } from 'react'
import { login } from '../services/testCaseViewerApi'
import { SESSION_KEY } from '../constants'

export default function LoginPage({ onLogin }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    if (!username.trim() || !password.trim()) {
      setError('Enter username and password.')
      return
    }

    setLoading(true)
    setError('')
    try {
      const response = await login(username.trim(), password)
      const user = response.user ?? { username: username.trim() }
      localStorage.setItem(SESSION_KEY, JSON.stringify(user))
      onLogin(user)
    } catch (err) {
      setError(err.message || 'Login failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div>
          <p className="eyebrow">Impact QA</p>
          <h1>TestCaseViewer</h1>
          <p className="login-copy">Sign in with your MongoDB users account to review master and regression coverage.</p>
        </div>

        <form className="login-form" onSubmit={submit}>
          <label>
            Username
            <input value={username} onChange={event => setUsername(event.target.value)} autoComplete="username" />
          </label>
          <label>
            Password
            <input
              value={password}
              onChange={event => setPassword(event.target.value)}
              type="password"
              autoComplete="current-password"
            />
          </label>
          {error && <div className="form-error">{error}</div>}
          <button type="submit" disabled={loading}>{loading ? 'Signing in...' : 'Sign in'}</button>
        </form>
      </section>
    </main>
  )
}
