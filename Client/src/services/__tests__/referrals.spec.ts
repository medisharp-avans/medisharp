import { afterEach, beforeEach, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import { createReferral, type ReferralDraft } from '../referrals'

afterEach(() => vi.unstubAllGlobals())

beforeEach(() => {
  setActivePinia(createPinia())
  const auth = useAuthStore()
  auth.token = 'test-access-token'
  auth.expiresAt = new Date(Date.now() + 120_000).toISOString()
})

it('verstuurt bijlagen als multipart zonder een eigen boundary-header', async () => {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 201 }))
  vi.stubGlobal('fetch', fetchMock)
  const file = new File(['%PDF-1.4\n%%EOF'], 'test.pdf', { type: 'application/pdf' })
  await createReferral(draft, [file])
  const [url, options] = fetchMock.mock.calls[0]!
  expect(url).toBe('/api/referrals/with-attachments')
  const headers = new Headers(options?.headers)
  expect(headers.has('Content-Type')).toBe(false)
  expect(headers.get('Authorization')).toBe('Bearer test-access-token')
  const form = options?.body as FormData
  expect(form.get('referral')).toBe(JSON.stringify(draft))
  expect(form.getAll('attachments')).toHaveLength(1)
})
const draft = { bsn: '000000001', patientName: 'Fictieve patiënt' } as ReferralDraft

it('verstuurt invoer in de body en accepteert uitsluitend 201 als bevestiging', async () => {
  const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 201 }))
  vi.stubGlobal('fetch', fetchMock)
  expect(await createReferral(draft)).toEqual({ saved: true })
  expect(fetchMock).toHaveBeenCalledWith('/api/referrals', {
    method: 'POST',
    headers: new Headers({
      'Content-Type': 'application/json',
      Authorization: 'Bearer test-access-token',
    }),
    body: JSON.stringify(draft),
    cache: 'no-store',
  })
})

it('verstuurt geen gegevens wanneer de sessie verlopen is', async () => {
  useAuthStore().token = null
  useAuthStore().refreshToken = null
  const fetchMock = vi.fn<typeof fetch>()
  vi.stubGlobal('fetch', fetchMock)

  const result = await createReferral(draft)
  expect(result.saved).toBe(false)
  expect(fetchMock).not.toHaveBeenCalled()
})

it('geeft veldnamen door zonder aangeleverde servertekst te tonen', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue({
      status: 400,
      json: async () => ({ errors: { bsn: ['Technische of gevoelige servertekst'] } }),
    }),
  )
  expect(await createReferral(draft)).toEqual({
    saved: false,
    fields: ['bsn'],
    message: 'Controleer de ingevulde gegevens.',
  })
})

it.each([404, 500, 200])('behandelt status %s niet als opgeslagen', async (status) => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ status }))
  expect((await createReferral(draft)).saved).toBe(false)
})

it('meldt dat opslag onzeker is wanneer de verbinding wegvalt', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Verbindingsdetails')))
  const result = await createReferral(draft)
  expect(result.saved).toBe(false)
  expect(JSON.stringify(result)).toContain('voordat je opnieuw verstuurt')
  expect(JSON.stringify(result)).not.toContain('Verbindingsdetails')
})
