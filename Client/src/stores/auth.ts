import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import api from '@/services/api'

export interface AuthUser {
  id: number
  username: string
  fullName: string
  email: string
  role: number | string
}

interface LoginResponse {
  token: string
  expiresAt: string
  user: AuthUser
}

const TOKEN_KEY = 'auth_token'
const EXPIRES_KEY = 'auth_expires_at'

function readStorage(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

// Houdt het JWT en de ingelogde gebruiker bij. Het token wordt in localStorage bewaard
// zodat de gebruiker ingelogd blijft na een refresh.
export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(readStorage(TOKEN_KEY))
  const expiresAt = ref<string | null>(readStorage(EXPIRES_KEY))
  const user = ref<AuthUser | null>(null)

  const isAuthenticated = computed(
    () => !!token.value && (!expiresAt.value || new Date(expiresAt.value) > new Date()),
  )

  async function login(username: string, password: string): Promise<void> {
    // skipErrorRedirect: een 401 betekent hier "verkeerde gegevens", geen redirect naar /401.
    const { data } = await api.post<LoginResponse>(
      '/api/login',
      { username, password },
      { skipErrorRedirect: true },
    )
    token.value = data.token
    expiresAt.value = data.expiresAt
    user.value = data.user
    try {
      localStorage.setItem(TOKEN_KEY, data.token)
      localStorage.setItem(EXPIRES_KEY, data.expiresAt)
    } catch {
      // Opslag niet beschikbaar: alleen ingelogd voor deze sessie.
    }
  }

  function logout(): void {
    token.value = null
    expiresAt.value = null
    user.value = null
    try {
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(EXPIRES_KEY)
    } catch {
      // negeren
    }
  }

  return { token, expiresAt, user, isAuthenticated, login, logout }
})
