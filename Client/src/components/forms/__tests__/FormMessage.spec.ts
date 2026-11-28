import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import FormMessage from '@/components/forms/FormMessage.vue'

describe('FormMessage', () => {
  it('does not render when message is null', () => {
    const wrapper = mount(FormMessage, {
      props: { message: null },
    })

    expect(wrapper.find('[data-testid="message"]').exists()).toBe(false)
  })

  it('shows an error message with alert role', () => {
    const wrapper = mount(FormMessage, {
      props: {
        message: {
          type: 'error',
          text: 'Er is iets fout gegaan.',
        },
      },
    })

    const message = wrapper.find('[data-testid="message"]')

    expect(message.text()).toBe('Er is iets fout gegaan.')
    expect(message.attributes('role')).toBe('alert')
  })

  it('shows a success message with status role', () => {
    const wrapper = mount(FormMessage, {
      props: {
        message: {
          type: 'success',
          text: 'De afspraak is opgeslagen.',
        },
      },
    })

    const message = wrapper.find('[data-testid="message"]')

    expect(message.text()).toBe('De afspraak is opgeslagen.')
    expect(message.attributes('role')).toBe('status')
  })

  it('uses a custom test id', () => {
    const wrapper = mount(FormMessage, {
      props: {
        message: {
          type: 'error',
          text: 'Verplicht veld.',
        },
        testid: 'appointment-error',
      },
    })

    expect(wrapper.find('[data-testid="appointment-error"]').exists()).toBe(true)
  })
})
