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
  fullName: 'Jan de Vries',
  email: 'jan@example.com',
  role: 'Patient',
}

const challenge = {
  challengeId: 'abc',
  expiresAt: new Date().toISOString(),
  maskedEmail: 'j***@example.com',
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
    post.mockResolvedValue({ data: challenge })
    const auth = useAuthStore()

    await auth.login('jan@example.com', 'Geheim123!')

    expect(post).toHaveBeenCalledWith(
      '/api/login',
      { email: 'jan@example.com', password: 'Geheim123!' },
      { skipErrorRedirect: true },
    )
  })

  it('geeft na stap 1 de challenge terug, maar is nog niet ingelogd (AC-PRI-04_1)', async () => {
    post.mockResolvedValue({ data: challenge })
    const auth = useAuthStore()

    const result = await auth.login('jan@example.com', 'Geheim123!')

    expect(result).toEqual(challenge)
    expect(auth.token).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
  })

  it('post de code naar /api/login/verify zonder error-redirect', async () => {
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt: futureDate(), user } })
    const auth = useAuthStore()

    await auth.verifyCode('abc', '123456')

    expect(post).toHaveBeenCalledWith(
      '/api/login/verify',
      { challengeId: 'abc', code: '123456' },
      { skipErrorRedirect: true },
    )
  })

  it('blijft uitgelogd bij een onjuiste code (AC-PRI-04_2)', async () => {
    const error = Object.assign(new Error('Unauthorized'), { response: { status: 401 } })
    post.mockRejectedValue(error)
    const auth = useAuthStore()

    await expect(auth.verifyCode('abc', '000000')).rejects.toBe(error)

    expect(auth.token).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('jwt_token')).toBeNull()
  })

  it('bewaart token, vervaldatum en gebruiker na een juiste code', async () => {
    const expiresAt = futureDate()
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt, user } })
    const auth = useAuthStore()

    await auth.verifyCode('abc', '123456')

    expect(auth.token).toBe('jwt')
    expect(auth.expiresAt).toBe(expiresAt)
    expect(auth.user).toEqual(user)
    expect(auth.isAuthenticated).toBe(true)
    expect(sessionStorage.getItem('jwt_token')).toBe('jwt')
    expect(sessionStorage.getItem('auth_expires_at')).toBe(expiresAt)
  })

  it('laat de fout door en blijft uitgelogd bij een mislukte login', async () => {
    const error = Object.assign(new Error('Unauthorized'), { response: { status: 401 } })
    post.mockRejectedValue(error)
    const auth = useAuthStore()

    await expect(auth.login('jan@example.com', 'fout')).rejects.toBe(error)

    expect(auth.token).toBeNull()
    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('jwt_token')).toBeNull()
  })

  it('blijft ingelogd voor de sessie als sessionStorage niet beschikbaar is', async () => {
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt: futureDate(), user } })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('QuotaExceededError')
    })
    const auth = useAuthStore()

    await auth.verifyCode('abc', '123456')

    expect(auth.token).toBe('jwt')
    expect(auth.isAuthenticated).toBe(true)
  })

  it('leest een opgeslagen, geldig token in na een refresh', () => {
    sessionStorage.setItem('jwt_token', 'jwt')
    sessionStorage.setItem('auth_expires_at', futureDate())

    const auth = useAuthStore()

    expect(auth.token).toBe('jwt')
    expect(auth.isAuthenticated).toBe(true)
  })

  it('is niet ingelogd met een verlopen token', () => {
    sessionStorage.setItem('jwt_token', 'jwt')
    sessionStorage.setItem('auth_expires_at', new Date(Date.now() - 1000).toISOString())

    const auth = useAuthStore()

    expect(auth.isAuthenticated).toBe(false)
  })

  it('wist alles bij uitloggen', async () => {
    post.mockResolvedValue({ data: { token: 'jwt', expiresAt: futureDate(), user } })
    const auth = useAuthStore()
    await auth.verifyCode('abc', '123456')

    auth.logout()

    expect(auth.token).toBeNull()
    expect(auth.expiresAt).toBeNull()
    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('jwt_token')).toBeNull()
    expect(sessionStorage.getItem('auth_expires_at')).toBeNull()
  })
})
