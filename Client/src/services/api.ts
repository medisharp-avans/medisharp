import axios from 'axios'
import router from '../router'

declare module 'axios' {
  interface AxiosRequestConfig {
    skipErrorRedirect?: boolean
  }
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7004',
  timeout: 10000,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (!axios.isAxiosError(error)) return Promise.reject(error)

    // Geen response: netwerkfout of server is down
    if (!error.response) {
      if (error.request) router.push({ name: '500' })
      return Promise.reject(error)
    }

    if (error.config?.skipErrorRedirect) return Promise.reject(error)

    const status = error.response.status

    switch (status) {
      case 400: router.push({ name: '400' }); break
      case 401: router.push({ name: '401' }); break
      case 403: router.push({ name: '403' }); break
      case 404: router.push({ name: '404' }); break
      case 500: router.push({ name: '500' }); break
      default:
        console.error(`Onbekende foutcode (${status}):`, error.response.data)
    }

    return Promise.reject(error)
  },
)
console.log('API URL:', api.defaults.baseURL)
export default api