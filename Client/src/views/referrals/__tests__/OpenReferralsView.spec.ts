import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { flushPromises, mount } from '@vue/test-utils'
import { useAuthStore } from '@/stores/auth'
import { getOpenReferrals } from '@/services/openReferrals'
import OpenReferralsView from '../OpenReferralsView.vue'

const row = {
  specialty: 'KNO',
  treatmentCode: 'Z0005',
  patientName: 'Fictieve patiënt',
  bsn: '000000001',
}
const fetchMock = vi.fn<typeof fetch>()
const view = () => mount(OpenReferralsView, { global: { stubs: { RouterLink: true } } })

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status })
}

beforeEach(() => {
  sessionStorage.clear()
  setActivePinia(createPinia())
  const auth = useAuthStore()
  auth.token = 'test-token'
  auth.expiresAt = new Date(Date.now() + 120_000).toISOString()
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => vi.unstubAllGlobals())

describe('Open verwijzingen', () => {
  it('toont de rij en vraagt de juiste pagina op bij vooruit en terug gaan', async () => {
    fetchMock.mockResolvedValueOnce(json(Array.from({ length: 25 }, () => row)))
    fetchMock.mockResolvedValueOnce(json([]))
    fetchMock.mockResolvedValueOnce(json([row]))
    const wrapper = view()
    expect(wrapper.text()).toContain('Verwijzingen laden')
    await flushPromises()
    expect(wrapper.get('tbody').text()).toContain(row.patientName)
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Geen open verwijzingen')
    expect(wrapper.text()).toContain('Pagina 2')
    await wrapper.get('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Pagina 1')
    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([
      '/api/referrals/open?page=1',
      '/api/referrals/open?page=2',
      '/api/referrals/open?page=1',
    ])
    expect(new Headers(fetchMock.mock.calls[0]?.[1]?.headers).get('Authorization')).toBe(
      'Bearer test-token',
    )
  })

  it.each([401, 403])('toont bij %s een toegangsmelding', async (status) => {
    fetchMock.mockResolvedValueOnce(json({}, status))
    const wrapper = view()
    await flushPromises()
    expect(wrapper.get('[role="alert"]').text()).toContain('bevoegd ziekenhuispersoneel')
    expect(wrapper.find('table').exists()).toBe(false)
  })

  it.each([
    null,
    [null],
    [{ ...row, specialty: 1 }],
    [{ ...row, treatmentCode: null }],
    [{ ...row, patientName: null }],
    [{ ...row, bsn: 1 }],
  ])('weigert onvolledige of ongeldige resultaten', async (body) => {
    fetchMock.mockResolvedValueOnce(json(body))
    await expect(getOpenReferrals(1)).rejects.toThrow(
      'De open verwijzingen konden niet worden opgehaald.',
    )
  })

  it('toont een veilige boodschap bij een serverfout', async () => {
    fetchMock.mockResolvedValueOnce(json({ error: 'interne details' }, 500))
    const wrapper = view()
    await flushPromises()
    expect(wrapper.get('[role="alert"]').text()).toBe(
      'De open verwijzingen konden niet worden opgehaald.',
    )
  })
})
