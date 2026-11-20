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
  for (const input of wrapper.findAll('input, textarea')) {
    await input.setValue(input.attributes('type') === 'email' ? 'test@example.invalid' : 'Fictief')
  }
  return { wrapper, post, finish: () => finish?.() }
}

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
