// Single source of truth for the pages that Lighthouse and axe test:
// every static path from the Vue router (no params, no redirects).
import routes from '../src/router/routes.ts'

// `vite preview` serves the production build on this port.
export const previewPort = 4173
export const baseUrl = `http://localhost:${previewPort}`

export const paths: string[] = routes
  .filter((route) => !('redirect' in route) && !route.path.includes(':'))
  .map((route) => route.path)
