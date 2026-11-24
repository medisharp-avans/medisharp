import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

// De echte api-module importeert de router; vervang hem door een simpele mock.
vi.mock('@/services/api', () => ({
  default: { post: vi.fn() },
}))

import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'

const post = vi.mocked(api.post)

const user = {
  id: 5,
  username: 'jdevries',
  fullName: 'Jan de Vries',
  email: 'jan@example.com',
  role: 'Patient',
}

function futureDate(): string {
  return new Date(Date.now() + 60 * 60 * 1000).toISOString()
}

describe('useAuthStore', () => {
  beforeEach(() => {
    sessionStorage.clear()
    post.mockReset()
    setActivePinia(createPinia())
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('is niet ingelogd zonder opgeslagen token', () => {
    const auth = useAuthStore()

    expect(auth.token).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
  })

  it('post de gegevens naar /api/login zonder error-redirect', async () => {
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt: futureDate(), user } })
    const auth = useAuthStore()

    await auth.login('jdevries', 'Geheim123!')

    expect(post).toHaveBeenCalledWith(
      '/api/login',
      { username: 'jdevries', password: 'Geheim123!' },
      { skipErrorRedirect: true },
    )
  })

  it('bewaart token, vervaldatum en gebruiker na een geslaagde login', async () => {
    const expiresAt = futureDate()
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt, user } })
    const auth = useAuthStore()

    await auth.login('jdevries', 'Geheim123!')

    expect(auth.token).toBe('jwt')
    expect(auth.expiresAt).toBe(expiresAt)
    expect(auth.user).toEqual(user)
    expect(auth.isAuthenticated).toBe(true)
    expect(sessionStorage.getItem('auth_token')).toBe('jwt')
    expect(sessionStorage.getItem('auth_expires_at')).toBe(expiresAt)
  })

  it('laat de fout door en blijft uitgelogd bij een mislukte login', async () => {
    const error = Object.assign(new Error('Unauthorized'), { response: { status: 401 } })
    post.mockRejectedValue(error)
    const auth = useAuthStore()

    await expect(auth.login('jdevries', 'fout')).rejects.toBe(error)

    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('auth_token')).toBeNull()
  })

  it('blijft ingelogd voor de sessie als localStorage niet beschikbaar is', async () => {
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt: futureDate(), user } })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('QuotaExceededError')
    })
    const auth = useAuthStore()

    await auth.login('jdevries', 'Geheim123!')

    expect(auth.token).toBe('jwt')
    expect(auth.isAuthenticated).toBe(true)
  })

  it('leest een opgeslagen, geldig token in na een refresh', () => {
    sessionStorage.setItem('auth_token', 'jwt')
    sessionStorage.setItem('auth_expires_at', futureDate())

    const auth = useAuthStore()

    expect(auth.token).toBe('jwt')
    expect(auth.isAuthenticated).toBe(true)
  })

  it('is niet ingelogd met een verlopen token', () => {
    sessionStorage.setItem('auth_token', 'jwt')
    sessionStorage.setItem('auth_expires_at', new Date(Date.now() - 1000).toISOString())

    const auth = useAuthStore()

    expect(auth.isAuthenticated).toBe(false)
  })

  it('wist alles bij uitloggen', async () => {
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt: futureDate(), user } })
    const auth = useAuthStore()
    await auth.login('jdevries', 'Geheim123!')

    auth.logout()

    expect(auth.token).toBeNull()
    expect(auth.expiresAt).toBeNull()
    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('auth_token')).toBeNull()
    expect(sessionStorage.getItem('auth_expires_at')).toBeNull()
  })
})
