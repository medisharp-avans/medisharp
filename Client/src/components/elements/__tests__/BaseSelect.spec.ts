import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseSelect from '@/components/elements/BaseSelect.vue'

const options = [
  { value: 1, label: 'Een' },
  { value: 2, label: 'Twee' },
]

describe('BaseSelect', () => {
  it('links the label to the select and gives the select an id and name', () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'number', label: 'Nummer', options, modelValue: 1 },
    })
    const select = wrapper.find('select')

    expect(wrapper.find('label').attributes('for')).toBe('number')
    expect(select.attributes('id')).toBe('number')
    expect(select.attributes('name')).toBe('number')
    expect(wrapper.findAll('option').map((o) => o.text())).toEqual(['Een', 'Twee'])
  })

  it('starts with the empty option when emptyLabel is set', () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'number', label: 'Nummer', options, modelValue: null, emptyLabel: 'Alle' },
    })

    expect(wrapper.findAll('option').map((o) => o.text())).toEqual(['Alle', 'Een', 'Twee'])
  })

  it('emits the value of the chosen option, keeping its type', async () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'number', label: 'Nummer', options, modelValue: null, emptyLabel: 'Alle' },
      attrs: { 'data-testid': 'my-select' },
    })

    await wrapper.find('[data-testid="my-select"]').setValue(2)

    expect(wrapper.emitted('update:modelValue')).toEqual([[2]])
  })

  it('can hide the label visually but keeps it for screen readers', () => {
    const wrapper = mount(BaseSelect, {
      props: { id: 'number', label: 'Nummer', options, modelValue: 1, hideLabel: true },
    })

    expect(wrapper.find('label').classes()).toContain('sr-only')
  })
})
