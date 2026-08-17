import { useState } from 'react'
import './App.css'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import { SESSION_KEY } from './constants'

export default function App() {
  const storedUser = (() => {
    try {
      return JSON.parse(localStorage.getItem(SESSION_KEY) || 'null')
    } catch {
      return null
    }
  })()
  const [user, setUser] = useState(storedUser)

  function logout() {
    localStorage.removeItem(SESSION_KEY)
    setUser(null)
  }

  return user ? <DashboardPage user={user} onLogout={logout} /> : <LoginPage onLogin={setUser} />
}
