import { isAxiosError } from 'axios'

/** Resultaat van create/update/delete: óf data, óf een fout. Controleer eerst result.error. */
export type ApiResult<T> = { data: T; error: null } | { data: null; error: unknown }

export const DEFAULT_ERROR_MESSAGE = 'Er is iets misgegaan.'

export function messageOf(err: unknown): string {
  if (isAxiosError(err)) {
    const body = err.response?.data
    if (typeof body === 'string' && body.trim()) return body
    if (body && typeof body === 'object') {
      const { message, detail, title } = body as Record<string, unknown>
      for (const candidate of [message, detail, title]) {
        if (typeof candidate === 'string' && candidate.trim()) return candidate
      }
    }
  }
  return err instanceof Error && err.message ? err.message : DEFAULT_ERROR_MESSAGE
}
