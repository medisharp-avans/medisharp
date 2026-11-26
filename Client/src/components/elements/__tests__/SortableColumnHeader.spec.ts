import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SortableColumnHeader from '@/components/elements/SortableColumnHeader.vue'

const mountHeader = (props: { active: boolean; descending?: boolean }) =>
  mount(SortableColumnHeader, {
    props: { label: 'Patiënt', ...props },
    attachTo: document.createElement('tr'),
  })

describe('SortableColumnHeader', () => {
  it('tells screen readers whether and how the column is sorted', () => {
    expect(mountHeader({ active: false }).find('th').attributes('aria-sort')).toBe('none')
    expect(mountHeader({ active: true }).find('th').attributes('aria-sort')).toBe('ascending')
    expect(mountHeader({ active: true, descending: true }).find('th').attributes('aria-sort')).toBe(
      'descending',
    )
  })

  it('emits sort when the button is clicked', async () => {
    const wrapper = mountHeader({ active: false })

    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('sort')).toHaveLength(1)
  })
})
