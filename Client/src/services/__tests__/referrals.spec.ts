import { afterEach, expect, it, vi } from 'vitest'
import { createReferral, type ReferralDraft } from '../referrals'

afterEach(() => vi.unstubAllGlobals())
const draft = { bsn: '000000001', patientName: 'Fictieve patiënt' } as ReferralDraft

it('verstuurt invoer in de body en accepteert uitsluitend 201 als bevestiging', async () => {
  const fetchMock = vi.fn().mockResolvedValue({ status: 201 })
  vi.stubGlobal('fetch', fetchMock)
  expect(await createReferral(draft)).toEqual({ saved: true })
  expect(fetchMock).toHaveBeenCalledWith('/api/referrals', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(draft),
  })
})

it('geeft veldnamen door zonder aangeleverde servertekst te tonen', async () => {
  vi.stubGlobal(
    'fetch',
    vi
      .fn()
      .mockResolvedValue({
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
