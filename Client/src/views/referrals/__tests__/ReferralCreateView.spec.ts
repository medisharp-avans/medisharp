import { afterEach, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import ReferralCreateView from '../ReferralCreateView.vue'

afterEach(() => vi.unstubAllGlobals())

async function setup(status = 201, delayed = false) {
  let finish: (() => void) | undefined
  const post = vi.fn(() =>
    delayed
      ? new Promise<Response>((resolve) => {
          finish = () => resolve(new Response(null, { status }))
        })
      : Promise.resolve(new Response(JSON.stringify({ errors: { bsn: ['Invalid'] } }), { status })),
  )
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, options?: RequestInit) => {
      if (options?.method === 'POST') return post()
      return Promise.resolve(
        new Response(
          JSON.stringify(
            url.includes('specialties')
              ? ['KNO', 'Oogheelkunde']
              : [
                  {
                    code: 'Z0005',
                    description: 'Consult',
                    specialty: new URL(url, 'http://localhost').searchParams.get('specialty'),
                  },
                ],
          ),
        ),
      )
    }),
  )
  const wrapper = mount(ReferralCreateView, {
    global: { stubs: { RouterLink: { template: '<a><slot /></a>' } } },
  })
  await flushPromises()
  await wrapper.get('#referral-specialty').setValue('KNO')
  await flushPromises()
  await wrapper.get('#referral-treatment').setValue('Z0005')
  for (const input of wrapper.findAll(
    'input:not([type=file]):not([type=search]):not([data-pending-entry]), textarea',
  )) {
    const values: Record<string, string> = {
      'patient-bsn': '000000000',
      'patient-email': 'test@example.invalid',
    }
    await input.setValue(values[input.attributes('id') ?? ''] ?? 'Fictief')
  }
  await wrapper.get('#referral-medication').setValue('Geen medicatie')
  await wrapper.get('#referral-allergies').setValue('Onbekend')
  return { wrapper, post, finish: () => finish?.() }
}

it('vult de contactgegevens in op exact BSN en wist ze bij een ander BSN', async () => {
  const { wrapper } = await setup()
  const fetchMock = vi.mocked(fetch)
  fetchMock.mockResolvedValueOnce(
    new Response(
      JSON.stringify({
        patientName: 'Testpatiënt',
        patientPostalCode: '1234 AB',
        patientHouseNumber: '12A',
        patientPhoneNumber: null,
        patientEmailAddress: 'patient@example.test',
      }),
    ),
  )

  await wrapper.get('#patient-bsn').setValue('012345678')
  await wrapper.get('#patient-lookup').trigger('click')
  await flushPromises()

  expect(fetchMock).toHaveBeenLastCalledWith(
    '/api/referrals/patient-lookup',
    expect.objectContaining({
      body: JSON.stringify({ bsn: '012345678' }),
      method: 'POST',
      cache: 'no-store',
    }),
  )
  const value = (id: string) => wrapper.get<HTMLInputElement>(id).element.value
  expect(value('#patient-name')).toBe('Testpatiënt')
  expect(value('#patient-postal-code')).toBe('1234 AB')
  expect(value('#patient-house-number')).toBe('12A')
  expect(value('#patient-phone')).toBe('')
  expect(value('#patient-email')).toBe('patient@example.test')
  await wrapper.get('#patient-bsn').setValue('012345679')
  expect(value('#patient-name')).toBe('')
  expect(value('#patient-email')).toBe('')
  wrapper.unmount()
})

it('laat een huisarts bij een onbekend BSN handmatig doorgaan', async () => {
  const { wrapper, post } = await setup()
  vi.mocked(fetch).mockResolvedValueOnce(new Response(null, { status: 204 }))

  await wrapper.get('#patient-lookup').trigger('click')
  await flushPromises()

  expect(wrapper.get('[role="status"]').text()).toContain('Geen patiënt gevonden')
  await wrapper.get('#patient-name').setValue('Nieuwe testpatiënt')
  await wrapper.get('form').trigger('submit')
  await flushPromises()
  expect(post).toHaveBeenCalledOnce()
  wrapper.unmount()
})

it('blokkeert dubbele verzending en wist de invoer na bevestiging', async () => {
  const { wrapper, post, finish } = await setup(201, true)
  await wrapper.get('form').trigger('submit')
  await wrapper.get('form').trigger('submit')
  expect(post).toHaveBeenCalledTimes(1)
  expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined()
  finish()
  await flushPromises()
  expect(wrapper.text()).toContain('opgeslagen met status Open')
  expect(wrapper.find('form').exists()).toBe(false)
  wrapper.unmount()
})

it('slaat een verwijzing op zonder telefoonnummer', async () => {
  const { wrapper, post } = await setup()
  const phone = wrapper.get('#patient-phone')
  expect(phone.attributes('required')).toBeUndefined()
  await phone.setValue('')
  await wrapper.get('form').trigger('submit')
  await flushPromises()
  expect(post).toHaveBeenCalledTimes(1)
  expect(wrapper.text()).toContain('opgeslagen met status Open')
  wrapper.unmount()
})

it('verstuurt meerdere medicijnen en een zelf toegevoegd allergeen met één verwijsbericht', async () => {
  const { wrapper } = await setup()
  const fetchMock = vi.mocked(fetch)
  const medication = wrapper.get('#referral-medication')
  await wrapper.get('#referral-medication-search').setValue('para')
  const result = wrapper.findAll('button').find((button) => button.text() === 'Paracetamol')
  expect(result).toBeDefined()
  await result!.trigger('click')
  await medication.setValue('Metformine')
  const custom = wrapper.get('#referral-allergies-custom')
  await custom.setValue('Fictieve stof')
  expect((custom.element as HTMLInputElement).checkValidity()).toBe(false)
  await custom.trigger('keydown', { key: 'Enter' })
  await wrapper.get('#referral-reason').setValue('Reden: klachten. Vraag: onderzoek mogelijk?')
  await wrapper.get('form').trigger('submit')
  await flushPromises()

  const request = fetchMock.mock.calls.find(([, options]) => options?.method === 'POST')?.[1]
  expect(JSON.parse(String(request?.body))).toMatchObject({
    medication: 'Paracetamol\nMetformine',
    allergies: 'Fictieve stof',
    reasonForReferral: 'Reden: klachten. Vraag: onderzoek mogelijk?',
  })
  expect(JSON.parse(String(request?.body))).not.toHaveProperty('clinicalQuestion')
  wrapper.unmount()
})

it.each([400, 500])('behoudt invoer bij status %s en toont een fout', async (status) => {
  const { wrapper } = await setup(status)
  await wrapper.get('form').trigger('submit')
  await flushPromises()
  expect(wrapper.get('[role="alert"]').text()).toBeTruthy()
  expect((wrapper.get('#patient-name').element as HTMLInputElement).value).toBe('Fictief')
  expect(wrapper.get('fieldset').attributes('disabled')).toBeUndefined()
  wrapper.unmount()
})

it('weigert witruimte en wist de behandeling bij een ander specialisme', async () => {
  const { wrapper, post } = await setup()
  await wrapper.get('#patient-name').setValue('   ')
  await wrapper.get('form').trigger('submit')
  expect(post).not.toHaveBeenCalled()
  expect(wrapper.get('[role="alert"]').text()).toContain('Naam patiënt')
  await wrapper.get('#referral-specialty').setValue('Oogheelkunde')
  await flushPromises()
  expect((wrapper.get('#referral-treatment').element as HTMLSelectElement).value).toBe('')
  wrapper.unmount()
})
