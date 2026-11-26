/**
 * All API endpoints in one place, so a view never contains a literal URL.
 * Paths are relative: the Vite dev proxy and nginx forward /api to the server.
 */
export const endpoints = {
  appointments: 'api/appointments',
  claims: {
    overview: 'api/Claims',
    toReviewCount: 'api/Claims/to-review-count',
  },
  planning: {
    patients: 'api/Planning/search/patients',
    specialists: 'api/Planning/search/specialists',
    treatmentCodes: 'api/Planning/treatmentcodes',
  },
} as const
