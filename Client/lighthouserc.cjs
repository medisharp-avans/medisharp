// Lighthouse CI config. URLs come from the Vue router via e2e/routes.ts,
// so new routes are tested automatically.
// Lighthouse CI only loads CommonJS configs, so require() is needed here.
// eslint-disable-next-line @typescript-eslint/no-require-imports
const { paths } = require('./e2e/routes.ts')

const baseUrl = 'http://localhost:4173'

module.exports = {
  ci: {
    collect: {
      startServerCommand: 'npm run preview -- --port 4173 --strictPort',
      startServerReadyPattern: 'Local',
      url: paths.map((path) => baseUrl + path),
      numberOfRuns: 1,
      settings: {
        preset: 'desktop',
        chromeFlags: '--headless=new --no-sandbox',
      },
    },
    assert: {
      assertions: {
        'categories:performance': ['warn', { minScore: 0.9 }],
        'categories:accessibility': ['error', { minScore: 0.95 }],
        'categories:best-practices': ['error', { minScore: 0.9 }],
        'categories:seo': ['warn', { minScore: 0.9 }],
      },
    },
    upload: {
      target: 'filesystem',
      outputDir: './lighthouse-reports',
    },
  },
}
