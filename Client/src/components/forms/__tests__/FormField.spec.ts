import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import FormField from '@/components/forms/FormField.vue'

describe('FormField', () => {
  it('connects the label to the input', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'name',
        label: 'Naam',
      },
      slots: {
        default: '<input id="name" />',
      },
    })

    expect(wrapper.find('label').attributes('for')).toBe('name')
    expect(wrapper.find('input').attributes('id')).toBe('name')
  })

  it('shows the optional label when optional is true', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'name',
        label: 'Naam',
        optional: true,
      },
    })

    expect(wrapper.text()).toContain('(optioneel)')
  })

  it('does not show the optional label by default', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'name',
        label: 'Naam',
      },
    })

    expect(wrapper.text()).not.toContain('(optioneel)')
  })

  it('shows the hint and connects it to the slot', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'email',
        label: 'E-mail',
        hint: 'Vul je e-mailadres in.',
      },
      slots: {
        default: `
          <input
            id="email"
            :aria-describedby="describedBy"
          />
        `,
      },
    })

    expect(wrapper.find('#email-hint').text()).toBe('Vul je e-mailadres in.')
    expect(wrapper.find('input').attributes('aria-describedby')).toBe('email-hint')
  })

  it('does not render a hint when none is provided', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'name',
        label: 'Naam',
      },
    })

    expect(wrapper.find('p').exists()).toBe(false)
  })

  it('passes the id and describedBy to the slot', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'phone',
        label: 'Telefoonnummer',
        hint: 'Vul je telefoonnummer in.',
      },
      slots: {
        default: `
          <div>
            <span data-testid="slot-id">{{ id }}</span>
            <span data-testid="slot-described-by">{{ describedBy }}</span>
          </div>
        `,
      },
    })

    expect(wrapper.find('[data-testid="slot-id"]').text()).toBe('phone')
    expect(wrapper.find('[data-testid="slot-described-by"]').text()).toBe('phone-hint')
  })
})
