/** Resultaat van create/update/delete: óf data, óf een fout. Controleer eerst result.error. */
export type ApiResult<T> = { data: T; error: null } | { data: null; error: unknown }

export const DEFAULT_ERROR_MESSAGE = 'Er is iets misgegaan.'

export function messageOf(err: unknown): string {
  return err instanceof Error && err.message ? err.message : DEFAULT_ERROR_MESSAGE
}
