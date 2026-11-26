import { createRouter, createWebHistory } from 'vue-router'
import routes from './routes.ts'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

// Every page gets its own browser tab title (WCAG 2.4.2), e.g. "Declaraties | MediSharp".
router.afterEach((to) => {
  const title = to.meta.title as string | undefined
  document.title = title ? `${title} | MediSharp` : 'MediSharp'
})

//TODO:
// router.BeforeEach, to intercept users for validation for authentication
// And possible rol defined meta information
export default router
