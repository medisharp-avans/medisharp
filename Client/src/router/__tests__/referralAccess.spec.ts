import { beforeEach, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore, type UserRole } from '@/stores/auth'
import router from '..'

beforeEach(async () => {
  sessionStorage.clear()
  setActivePinia(createPinia())
  await router.push('/')
})

function signIn(role: UserRole) {
  const auth = useAuthStore()
  auth.user = { id: 1, fullName: 'Fictieve gebruiker', email: 'test@example.test', role }
  auth.token = 'test-access-token'
  auth.expiresAt = new Date(Date.now() + 120_000).toISOString()
}

it('gaat naar PRI-login en onthoudt het verwijsscherm', async () => {
  await router.push('/verwijsbrieven/nieuw')
  expect(router.currentRoute.value.name).toBe('Login')
  expect(router.currentRoute.value.query.redirect).toBe('/verwijsbrieven/nieuw')
})

it.each([
  ['Huisarts', '/verwijsbrieven/nieuw', 'ReferralCreate'],
  ['Huisarts', '/verwijsbrieven/open', '403'],
  ['Ziekenhuismedewerker', '/verwijsbrieven/open', 'OpenReferrals'],
  ['Ziekenhuismedewerker', '/verwijsbrieven/nieuw', '403'],
  ['Patient', '/verwijsbrieven/nieuw', '403'],
] as const)('controleert de route voor %s op %s', async (role, path, expected) => {
  signIn(role)
  await router.push(path)
  expect(router.currentRoute.value.name).toBe(expected)
})
