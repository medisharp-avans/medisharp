import { createRouter, createWebHistory } from 'vue-router'
import routes from './routes.ts'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

//TODO:
// router.BeforeEach, to intercept users for validation for authentication
// And possible rol defined meta information
export default router
