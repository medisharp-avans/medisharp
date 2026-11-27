import { useAuthStore } from '@/stores/auth'

/** Uses the PRI session for requests containing referral data. */
export async function referralRequest(url: string, options: RequestInit = {}): Promise<Response> {
  const auth = useAuthStore()
  const headers = new Headers(options.headers)
  const token = await auth.getValidToken()
  if (!token) return new Response(null, { status: 401 })

  headers.set('Authorization', `Bearer ${token}`)
  return fetch(url, { ...options, headers, cache: 'no-store' })
}
