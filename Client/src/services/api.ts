import axios from 'axios'
import router from '../router'
import { useAuthStore } from '@/stores/auth'

declare module 'axios' {
  interface AxiosRequestConfig {
    skipErrorRedirect?: boolean
    /** Geen JWT meesturen en niet verversen (voor de login-, refresh- en logout-endpoints zelf). */
    skipAuthRefresh?: boolean
    /** Intern: deze request is al een keer opnieuw geprobeerd na een refresh. */
    _authRetried?: boolean
  }
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7004',
  timeout: 10000,
  headers: { 'Content-Type': 'application/json' },
})

// Stuur een geldig JWT mee met elke request. Is het (bijna) verlopen, dan vraagt de
// auth store eerst een nieuw JWT aan met de refresh token; zo blijf je ingelogd zolang je actief bent.
api.interceptors.request.use(async (config) => {
  if (config.skipAuthRefresh) return config
  const token = await useAuthStore().getValidToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error)) return Promise.reject(error)

    // Geen response: netwerkfout of server is down
    if (!error.response) {
      if (error.request) router.push({ name: '500' })
      return Promise.reject(error)
    }

    // JWT geweigerd (bv. verlopen op de server): één keer verversen en de request opnieuw proberen.
    const config = error.config
    if (
      error.response.status === 401 &&
      config &&
      !config.skipAuthRefresh &&
      !config._authRetried &&
      config.headers?.Authorization
    ) {
      config._authRetried = true
      if (await useAuthStore().refresh()) return api(config)
    }

    if (config?.skipErrorRedirect) return Promise.reject(error)

    const status = error.response.status

    switch (status) {
      case 400:
        router.push({ name: '400' })
        break
      case 401:
        router.push({ name: '401' })
        break
      case 403:
        router.push({ name: '403' })
        break
      case 404:
        router.push({ name: '404' })
        break
      case 500:
        router.push({ name: '500' })
        break
      default:
        console.error(`Onbekende foutcode (${status}):`, error.response.data)
    }

    return Promise.reject(error)
  },
)
console.log('API URL:', api.defaults.baseURL)
export default api
