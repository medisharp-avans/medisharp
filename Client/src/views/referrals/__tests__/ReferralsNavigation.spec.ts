import { expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import App from '@/App.vue'
import routes from '@/router/routes'
import { useAuthStore, type UserRole } from '@/stores/auth'

it.each([
  ['Huisarts', true, 'Nieuwe verwijzing'],
  ['Ziekenhuismedewerker', true, 'Open verwijzingen'],
  ['Patient', false, ''],
  [undefined, false, ''],
] as const)(
  'toont verwijsacties die bij de sessie passen',
  async (role: UserRole | undefined, visible, action) => {
    sessionStorage.clear()
    const router = createRouter({ history: createMemoryHistory(), routes })
    const pinia = createPinia()
    const auth = useAuthStore(pinia)
    if (role) {
      auth.user = { id: 1, fullName: 'Fictieve gebruiker', email: 'fictief@example.test', role }
      auth.token = 'test-token'
      auth.expiresAt = new Date(Date.now() + 120_000).toISOString()
    }
    await router.push('/verwijsbrieven')
    const wrapper = mount(App, { global: { plugins: [router, pinia] } })
    try {
      expect(wrapper.find('#ReferralsRoute-link').exists()).toBe(visible)
      const links = wrapper
        .get('main')
        .findAll('a')
        .map((link) => link.text())
      expect(links).toEqual(action ? [action] : [])
    } finally {
      wrapper.unmount()
    }
  },
)
