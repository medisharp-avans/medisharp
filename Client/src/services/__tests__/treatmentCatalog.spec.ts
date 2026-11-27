import { afterEach, describe, expect, it, vi } from 'vitest'
import { getTreatments } from '../treatmentCatalog'

afterEach(() => vi.unstubAllGlobals())

describe('getTreatments', () => {
  it('filtert op het specialisme en behoudt verschillende codes met dezelfde omschrijving', async () => {
    const choices = [
      { code: 'Z0005', description: 'Consult oren', specialty: 'KNO' },
      { code: 'Z0072', description: 'Consult oren', specialty: 'KNO' },
    ]
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(choices)))
    vi.stubGlobal('fetch', fetchMock)
    expect(await getTreatments('KNO')).toEqual(choices)
    expect(fetchMock).toHaveBeenCalledWith('/api/treatments?specialty=KNO', { signal: undefined })
  })

  it('weigert behandelingen van een ander specialisme', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [{ code: 'X', description: 'Test', specialty: 'Anders' }],
      }),
    )
    await expect(getTreatments('KNO')).rejects.toThrow('Ongeldige catalogusrespons')
  })

  it('geeft een fout bij een mislukte aanvraag en geeft annulering door', async () => {
    const controller = new AbortController()
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 503 }))
    vi.stubGlobal('fetch', fetchMock)
    await expect(getTreatments('KNO', controller.signal)).rejects.toThrow(
      'Catalogus niet beschikbaar',
    )
    expect(fetchMock).toHaveBeenCalledWith('/api/treatments?specialty=KNO', {
      signal: controller.signal,
    })
  })
})
