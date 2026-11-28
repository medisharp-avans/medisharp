import { createRouter, createWebHistory } from 'vue-router'
import routes from './routes.ts'
import { useAuthStore, type UserRole } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    referralRoles?: UserRole[]
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

router.beforeEach(async (to) => {
  const roles = to.meta.referralRoles
  if (!roles) return

  const auth = useAuthStore()
  if (!(await auth.getValidToken())) return { name: 'Login', query: { redirect: to.fullPath } }
  if (!auth.user || !roles.includes(auth.user.role)) return { name: '403' }
})

export default router
