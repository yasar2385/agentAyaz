export function isRunManager(user) {
  const role = typeof user?.role === 'string' ? user.role : JSON.stringify(user?.role ?? '')
  return role.toLowerCase().includes('dev') || role.toLowerCase().includes('manager')
}
