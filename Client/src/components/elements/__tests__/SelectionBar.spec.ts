import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SelectionBar from '@/components/elements/SelectionBar.vue'

const props = { singular: 'declaratie', plural: 'declaraties' }

describe('SelectionBar', () => {
  it('explains how to select when nothing is selected', () => {
    const wrapper = mount(SelectionBar, { props: { ...props, count: 0 } })

    expect(wrapper.find('[data-testid="selection-count"]').text()).toBe(
      'Selecteer declaraties met de vakjes in de tabel',
    )
    expect(wrapper.classes()).toContain('bg-white')
  })

  it('shows the count with the singular or plural name and turns blue', () => {
    const one = mount(SelectionBar, { props: { ...props, count: 1 } })
    const two = mount(SelectionBar, { props: { ...props, count: 2 } })

    expect(one.find('[data-testid="selection-count"]').text()).toContain(
      '1 declaratie geselecteerd',
    )
    expect(two.find('[data-testid="selection-count"]').text()).toContain(
      '2 declaraties geselecteerd',
    )
    expect(two.classes()).toContain('bg-blue-50')
  })

  it('shows the actions from the slot', () => {
    const wrapper = mount(SelectionBar, {
      props: { ...props, count: 0 },
      slots: { default: '<button>Actie</button>' },
    })

    expect(wrapper.find('button').text()).toBe('Actie')
  })
})
