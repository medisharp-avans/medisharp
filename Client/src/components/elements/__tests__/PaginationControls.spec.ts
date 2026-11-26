import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PaginationControls from '@/components/elements/PaginationControls.vue'

describe('PaginationControls', () => {
  it('is hidden when there is only one page', () => {
    const wrapper = mount(PaginationControls, { props: { page: 1, totalPages: 1 } })

    expect(wrapper.find('nav').exists()).toBe(false)
  })

  it('shows the current page and emits the next and previous page', async () => {
    const wrapper = mount(PaginationControls, { props: { page: 2, totalPages: 3 } })

    expect(wrapper.find('[data-testid="pagination-status"]').text()).toBe('Pagina 2 van 3')

    await wrapper.find('[data-testid="pagination-next"]').trigger('click')
    await wrapper.find('[data-testid="pagination-previous"]').trigger('click')

    expect(wrapper.emitted('update:page')).toEqual([[3], [1]])
  })

  it('disables previous on the first page and next on the last page', () => {
    const first = mount(PaginationControls, { props: { page: 1, totalPages: 2 } })
    const last = mount(PaginationControls, { props: { page: 2, totalPages: 2 } })

    expect(first.find('[data-testid="pagination-previous"]').attributes('disabled')).toBeDefined()
    expect(last.find('[data-testid="pagination-next"]').attributes('disabled')).toBeDefined()
  })

  it('disables both buttons while loading', () => {
    const wrapper = mount(PaginationControls, { props: { page: 2, totalPages: 3, loading: true } })

    expect(wrapper.find('[data-testid="pagination-previous"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[data-testid="pagination-next"]').attributes('disabled')).toBeDefined()
  })
})
