import { ref } from 'vue'
import type { AxiosRequestConfig } from 'axios'
import api from '@/services/api'
import { messageOf, type ApiResult } from './types'

export function useUpdate<TResponse, TRequest = unknown>(
  url: string,
  updateMethod: 'put' | 'patch' = 'put',
  config?: AxiosRequestConfig,
) {
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function update(newData: TRequest): Promise<ApiResult<TResponse>> {
    loading.value = true
    error.value = null
    try {
      const response = await api[updateMethod]<TResponse>(url, newData, config)
      return { data: response.data, error: null }
    } catch (err) {
      error.value = messageOf(err)
      return { data: null, error: err }
    } finally {
      loading.value = false
    }
  }

  return { update, loading, error }
}
