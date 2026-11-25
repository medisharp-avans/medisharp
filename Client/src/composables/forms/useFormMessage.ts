import { ref } from 'vue'
import axios from 'axios'

export interface FormMessageData {
  type: 'error' | 'success'
  text: string
}

/**
 * De melding boven of onder een formulier (zie FormMessage.vue).
 * `fail` toont de tekst die de server meestuurt (bijvoorbeeld bij een 409 Conflict)
 * en valt terug op een eigen tekst als die er niet is.
 */
export function useFormMessage() {
  const message = ref<FormMessageData | null>(null)

  function clear() {
    message.value = null
  }

  function success(text: string) {
    message.value = { type: 'success', text }
  }

  function error(text: string) {
    message.value = { type: 'error', text }
  }

  function fail(err: unknown, fallback: string) {
    error(
      axios.isAxiosError(err) && typeof err.response?.data === 'string'
        ? err.response.data
        : fallback,
    )
  }

  return { message, clear, success, error, fail }
}
