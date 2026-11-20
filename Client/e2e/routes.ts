// Single source of truth for the pages that Lighthouse and axe test:
// every static path from the Vue router (no params, no redirects).
import routes from '../src/router/routes.ts'

export const paths: string[] = routes
  .filter((route) => !('redirect' in route) && !route.path.includes(':'))
  .map((route) => route.path)
