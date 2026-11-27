import { ref } from 'vue'
import axios from 'axios'

export interface FormMessageData {
  type: 'error' | 'success'
  text: string
}

/**
 * Manages form feedback messages (such as success or error states) for display
 * in components like `FormMessage.vue`.
 *
 * Provides helper methods to clear, set success/error text directly, or handle
 * Axios errors with a custom fallback option.
 */
export function useFormMessage() {
  const message = ref<FormMessageData | null>(null)

  /** Clears the current form message. */
  function clear() {
    message.value = null
  }

  /** Sets a success message with the provided text. */
  function success(text: string) {
    message.value = { type: 'success', text }
  }

  /** Sets an error message with the provided text. */
  function error(text: string) {
    message.value = { type: 'error', text }
  }

  /**
   * Handles an error (typically from an API call), extracting server-provided
   * error text (e.g., from a 409 Conflict) or falling back to a default string.
   *
   * @param err - The caught error object, which may be an AxiosError.
   * @param fallback - The default message to display if no server message is available.
   */
  function fail(err: unknown, fallback: string) {
    error(
      axios.isAxiosError(err) && typeof err.response?.data === 'string'
        ? err.response.data
        : fallback,
    )
  }

  return { message, clear, success, error, fail }
}
