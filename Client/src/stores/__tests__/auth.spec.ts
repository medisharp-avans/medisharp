import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

// De echte api-module importeert de router; vervang hem door een simpele mock.
vi.mock('@/services/api', () => ({
  default: { post: vi.fn<typeof api.post>() },
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

function futureDate(minutes = 60): string {
  return new Date(Date.now() + minutes * 60 * 1000).toISOString()
}

function pastDate(): string {
  return new Date(Date.now() - 1000).toISOString()
}

function session(overrides: Record<string, unknown> = {}) {
  return {
    token: 'jwt',
    expiresAt: futureDate(5),
    refreshToken: 'refresh-1',
    refreshTokenExpiresAt: futureDate(30),
    user,
    ...overrides,
  }
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
      { skipErrorRedirect: true, skipAuthRefresh: true },
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
    post.mockResolvedValue({ data: session() })
    const auth = useAuthStore()

    await auth.verifyCode('abc', '123456')

    expect(post).toHaveBeenCalledWith(
      '/api/login/verify',
      { challengeId: 'abc', code: '123456' },
      { skipErrorRedirect: true, skipAuthRefresh: true },
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
    post.mockResolvedValue({ data: { ...session(), expiresAt } })
    const auth = useAuthStore()

    await auth.verifyCode('abc', '123456')

    expect(auth.token).toBe('jwt')
    expect(auth.expiresAt).toBe(expiresAt)
    expect(auth.user).toEqual(user)
    expect(auth.isAuthenticated).toBe(true)
    expect(sessionStorage.getItem('jwt_token')).toBe('jwt')
    expect(sessionStorage.getItem('auth_expires_at')).toBe(expiresAt)
    expect(auth.refreshToken).toBe('refresh-1')
    expect(sessionStorage.getItem('refresh_token')).toBe('refresh-1')
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
    post.mockResolvedValue({ data: session() })
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
    post.mockResolvedValue({ data: session() })
    const auth = useAuthStore()
    await auth.verifyCode('abc', '123456')

    post.mockResolvedValue({ data: undefined })
    await auth.logout()

    expect(post).toHaveBeenLastCalledWith(
      '/api/logout',
      { refreshToken: 'refresh-1' },
      { skipErrorRedirect: true, skipAuthRefresh: true },
    )
    expect(auth.token).toBeNull()
    expect(auth.refreshToken).toBeNull()
    expect(sessionStorage.getItem('refresh_token')).toBeNull()
    expect(auth.expiresAt).toBeNull()
    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
    expect(sessionStorage.getItem('jwt_token')).toBeNull()
    expect(sessionStorage.getItem('auth_expires_at')).toBeNull()
  })

  describe('refresh token', () => {
    function storeSession(accessExpiresAt: string, refreshExpiresAt = futureDate(30)) {
      sessionStorage.setItem('jwt_token', 'old-jwt')
      sessionStorage.setItem('auth_expires_at', accessExpiresAt)
      sessionStorage.setItem('refresh_token', 'refresh-1')
      sessionStorage.setItem('refresh_expires_at', refreshExpiresAt)
    }

    it('blijft ingelogd met een verlopen JWT zolang de refresh token geldig is', () => {
      storeSession(pastDate())

      expect(useAuthStore().isAuthenticated).toBe(true)
    })

    it('is uitgelogd als ook de refresh token verlopen is (30 minuten inactief)', () => {
      storeSession(pastDate(), pastDate())

      expect(useAuthStore().isAuthenticated).toBe(false)
    })

    it('geeft een geldig JWT terug zonder te verversen', async () => {
      storeSession(futureDate(5))

      expect(await useAuthStore().getValidToken()).toBe('old-jwt')
      expect(post).not.toHaveBeenCalled()
    })

    it('ververst een verlopen JWT en slaat de nieuwe tokens op', async () => {
      storeSession(pastDate())
      post.mockResolvedValue({ data: session({ token: 'new-jwt', refreshToken: 'refresh-2' }) })
      const auth = useAuthStore()

      expect(await auth.getValidToken()).toBe('new-jwt')

      expect(post).toHaveBeenCalledWith(
        '/api/login/refresh',
        { refreshToken: 'refresh-1' },
        { skipErrorRedirect: true, skipAuthRefresh: true },
      )
      expect(auth.refreshToken).toBe('refresh-2')
      expect(sessionStorage.getItem('jwt_token')).toBe('new-jwt')
      expect(sessionStorage.getItem('refresh_token')).toBe('refresh-2')
    })

    it('doet maar één refresh bij gelijktijdige requests', async () => {
      storeSession(pastDate())
      post.mockResolvedValue({ data: session({ token: 'new-jwt' }) })
      const auth = useAuthStore()

      const tokens = await Promise.all([auth.getValidToken(), auth.getValidToken(), auth.refresh()])

      expect(tokens).toEqual(['new-jwt', 'new-jwt', true])
      expect(post).toHaveBeenCalledTimes(1)
    })

    it('logt uit als de server de refresh token weigert', async () => {
      storeSession(pastDate())
      post.mockRejectedValue(
        Object.assign(new Error('Unauthorized'), { response: { status: 401 } }),
      )
      const auth = useAuthStore()

      expect(await auth.refresh()).toBe(false)

      expect(auth.isAuthenticated).toBe(false)
      expect(sessionStorage.getItem('refresh_token')).toBeNull()
    })

    it('ververst niet met een verlopen refresh token', async () => {
      storeSession(pastDate(), pastDate())
      const auth = useAuthStore()

      expect(await auth.getValidToken()).toBeNull()
      expect(post).not.toHaveBeenCalled()
    })
  })
})
