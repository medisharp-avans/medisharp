import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import api from '@/services/api'

export interface AuthUser {
  id: number
  fullName: string
  email: string
  role: UserRole
}

interface LoginResponse {
  token: string
  expiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: AuthUser
}

export interface LoginChallenge {
  challengeId: string
  expiresAt: string
  maskedEmail: string
}

export type UserRole =
  'Patient' | 'Huisarts' | 'Specialist' | 'Ziekenhuismedewerker' | 'Systeembeheerder'

export const ROLE_LABELS: Record<UserRole, string> = {
  Patient: 'Patiënt',
  Huisarts: 'Huisarts',
  Specialist: 'Specialist',
  Ziekenhuismedewerker: 'Ziekenhuismedewerker',
  Systeembeheerder: 'Systeembeheerder',
}

export const TOKEN_KEY = 'jwt_token'
const EXPIRES_KEY = 'auth_expires_at'
const REFRESH_KEY = 'refresh_token'
const REFRESH_EXPIRES_KEY = 'refresh_expires_at'
const USER_KEY = 'auth_user'
const STORAGE_KEYS = [TOKEN_KEY, EXPIRES_KEY, REFRESH_KEY, REFRESH_EXPIRES_KEY, USER_KEY]

/** Ververs het JWT als het binnen deze marge verloopt, zodat een request niet halverwege afloopt. */
const REFRESH_MARGIN_MS = 30_000

function readStorage(key: string): string | null {
  try {
    return sessionStorage.getItem(key)
  } catch {
    return null
  }
}

function readUser(): AuthUser | null {
  const raw = readStorage(USER_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

function isFuture(date: string | null, marginMs = 0): boolean {
  return !!date && new Date(date).getTime() - marginMs > Date.now()
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(readStorage(TOKEN_KEY))
  const expiresAt = ref<string | null>(readStorage(EXPIRES_KEY))
  const refreshToken = ref<string | null>(readStorage(REFRESH_KEY))
  const refreshExpiresAt = ref<string | null>(readStorage(REFRESH_EXPIRES_KEY))
  const user = ref<AuthUser | null>(readUser())

  const hasValidAccessToken = computed(
    () => !!token.value && (!expiresAt.value || isFuture(expiresAt.value)),
  )

  /** Ingelogd zolang het JWT geldig is, of zolang het met de refresh token vernieuwd kan worden. */
  const isAuthenticated = computed(
    () => hasValidAccessToken.value || (!!refreshToken.value && isFuture(refreshExpiresAt.value)),
  )

  const roleLabel = computed(() => (user.value ? (ROLE_LABELS[user.value.role] ?? null) : null))

  function applySession(data: LoginResponse): void {
    token.value = data.token
    expiresAt.value = data.expiresAt
    refreshToken.value = data.refreshToken
    refreshExpiresAt.value = data.refreshTokenExpiresAt
    user.value = data.user
    try {
      sessionStorage.setItem(TOKEN_KEY, data.token)
      sessionStorage.setItem(EXPIRES_KEY, data.expiresAt)
      sessionStorage.setItem(REFRESH_KEY, data.refreshToken)
      sessionStorage.setItem(REFRESH_EXPIRES_KEY, data.refreshTokenExpiresAt)
      sessionStorage.setItem(USER_KEY, JSON.stringify(data.user))
    } catch {
      // Opslag niet beschikbaar: alleen ingelogd zolang de pagina open is.
    }
  }

  function clearSession(): void {
    token.value = null
    expiresAt.value = null
    refreshToken.value = null
    refreshExpiresAt.value = null
    user.value = null
    try {
      STORAGE_KEYS.forEach((key) => sessionStorage.removeItem(key))
    } catch {
      // negeren
    }
  }

  async function login(email: string, password: string): Promise<LoginChallenge> {
    // skipErrorRedirect: een 401 betekent hier "verkeerde gegevens", geen redirect naar /401.
    const { data } = await api.post<LoginChallenge>(
      '/api/login',
      { email, password },
      { skipErrorRedirect: true, skipAuthRefresh: true },
    )
    return data
  }

  /** Stap 2: controleert de code uit de e-mail en slaat het JWT en de refresh token op. */
  async function verifyCode(challengeId: string, code: string): Promise<void> {
    const { data } = await api.post<LoginResponse>(
      '/api/login/verify',
      { challengeId, code },
      { skipErrorRedirect: true, skipAuthRefresh: true },
    )
    applySession(data)
  }

  let pendingRefresh: Promise<boolean> | null = null

  /**
   * Vraagt een nieuw JWT aan met de refresh token. Elke refresh verlengt de sessie met 30 minuten.
   */
  function refresh(): Promise<boolean> {
    if (pendingRefresh) return pendingRefresh
    const current = refreshToken.value
    if (!current || !isFuture(refreshExpiresAt.value)) {
      clearSession()
      return Promise.resolve(false)
    }

    pendingRefresh = api
      .post<LoginResponse>(
        '/api/login/refresh',
        { refreshToken: current },
        { skipErrorRedirect: true, skipAuthRefresh: true },
      )
      .then(({ data }) => {
        applySession(data)
        return true
      })
      .catch(() => {
        clearSession()
        return false
      })
      .finally(() => {
        pendingRefresh = null
      })
    return pendingRefresh
  }

  /** Geeft een geldig JWT terug (vernieuwt het eerst als het bijna verlopen is), of `null`. */
  async function getValidToken(): Promise<string | null> {
    if (token.value && isFuture(expiresAt.value, REFRESH_MARGIN_MS)) return token.value
    if (!refreshToken.value) return hasValidAccessToken.value ? token.value : null
    return (await refresh()) ? token.value : null
  }

  /** Beëindigt de sessie, ook op de server: de refresh token werkt daarna niet meer. */
  async function logout(): Promise<void> {
    const current = refreshToken.value
    clearSession()
    if (!current) return
    try {
      await api.post(
        '/api/logout',
        { refreshToken: current },
        { skipErrorRedirect: true, skipAuthRefresh: true },
      )
    } catch {
      // Lokaal is de sessie al gewist; de token verloopt anders vanzelf.
    }
  }

  return {
    token,
    expiresAt,
    refreshToken,
    refreshExpiresAt,
    user,
    isAuthenticated,
    roleLabel,
    login,
    verifyCode,
    refresh,
    getValidToken,
    logout,
  }
})
