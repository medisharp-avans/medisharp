import { ref, watch, toValue, type MaybeRefOrGetter, type Ref } from 'vue'
import axios from 'axios'
import api from '@/services/api'
import { messageOf } from './types'

type Params = Record<string, unknown>

export function useFetch<T>(url: string, params: MaybeRefOrGetter<Params> = () => ({})) {
  const data = ref<T | null>(null) as Ref<T | null>
  const loading = ref(false)
  const error = ref<string | null>(null)

  let controller: AbortController | null = null

  async function fetchData(): Promise<void> {
    controller?.abort()
    controller = new AbortController()
    const signal = controller.signal

    loading.value = true
    error.value = null
    try {
      const response = await api.get<T>(url, { params: toValue(params), signal })
      data.value = response.data
    } catch (err) {
      if (axios.isCancel(err)) return
      error.value = messageOf(err)
    } finally {
      if (!signal.aborted) loading.value = false
    }
  }

  watch(() => toValue(params), fetchData, { immediate: true, deep: true })

  return { data, loading, error, refetch: fetchData }
}
