import { describe, it, expect, beforeEach } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import AppHeader from '@/components/Layout/AppHeader.vue'
import { useMobileNavigation } from '@/composables/general/useMobileNavigation'

const mountHeader = () => mount(AppHeader, { global: { stubs: { RouterLink: RouterLinkStub } } })

describe('AppHeader', () => {
  beforeEach(() => useMobileNavigation().close())

  it('has no h1, so each page can use its own main heading', () => {
    expect(mountHeader().find('h1').exists()).toBe(false)
  })

  it('opens and closes the menu and tells screen readers whether it is open', async () => {
    const wrapper = mountHeader()
    const button = wrapper.find('#menu-button')

    expect(button.attributes('aria-expanded')).toBe('false')
    expect(button.attributes('aria-label')).toBe('Menu openen')

    await button.trigger('click')
    expect(useMobileNavigation().isOpen.value).toBe(true)
    expect(button.attributes('aria-expanded')).toBe('true')
    expect(button.attributes('aria-label')).toBe('Menu sluiten')

    await button.trigger('click')
    expect(useMobileNavigation().isOpen.value).toBe(false)
  })
})
