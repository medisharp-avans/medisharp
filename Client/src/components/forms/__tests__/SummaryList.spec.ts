import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SummaryList from '@/components/forms/SummaryList.vue'

const items = [
  { testid: 'name', label: 'Naam', value: 'Brian', step: 0 },
  { testid: 'specialism', label: 'Specialisme', value: 'Cardiologie', step: 1 },
  { testid: 'note', label: 'Opmerking', value: '' },
]

describe('SummaryList', () => {
  it('shows all summary items', () => {
    const wrapper = mount(SummaryList, {
      props: { items },
    })

    expect(wrapper.find('[data-testid="summary"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="summary-name"]').text()).toBe('Brian')
    expect(wrapper.find('[data-testid="summary-specialism"]').text()).toBe('Cardiologie')
  })

  it('shows a dash when a value is empty', () => {
    const wrapper = mount(SummaryList, {
      props: { items },
    })

    expect(wrapper.find('[data-testid="summary-note"]').text()).toBe('—')
  })

  it('only shows edit buttons when a step is provided', () => {
    const wrapper = mount(SummaryList, {
      props: { items },
    })

    expect(wrapper.find('[data-testid="summary-edit-name"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="summary-edit-specialism"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="summary-edit-note"]').exists()).toBe(false)
  })

  it('emits edit with the correct step when clicking edit', async () => {
    const wrapper = mount(SummaryList, {
      props: { items },
    })

    await wrapper.find('[data-testid="summary-edit-specialism"]').trigger('click')

    expect(wrapper.emitted('edit')).toEqual([[1]])
  })
})
