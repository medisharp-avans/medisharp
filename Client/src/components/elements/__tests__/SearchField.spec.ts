import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SearchField from '@/components/elements/SearchField.vue'

describe('SearchField', () => {
  it('links the label to the input and gives the input an id and name', () => {
    const wrapper = mount(SearchField, { props: { id: 'search', modelValue: '' } })
    const input = wrapper.find('input')

    expect(wrapper.find('label').attributes('for')).toBe('search')
    expect(input.attributes('id')).toBe('search')
    expect(input.attributes('name')).toBe('search')
  })

  it('puts extra attributes on the input and emits what the user types', async () => {
    const wrapper = mount(SearchField, {
      props: { id: 'search', modelValue: '' },
      attrs: { 'data-testid': 'my-search' },
    })

    await wrapper.find('[data-testid="my-search"]').setValue('Jansen')

    expect(wrapper.emitted('update:modelValue')).toEqual([['Jansen']])
  })
})
