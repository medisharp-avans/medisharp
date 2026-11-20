// Runs axe-core accessibility checks against every route of the built app.
// Expects `npm run preview` to be serving the build (default http://localhost:4173).
// Writes a JSON report per route to ./a11y-reports and exits 1 on any violation.
import { mkdir, writeFile } from 'node:fs/promises'
import { chromium } from 'playwright'
import { AxeBuilder } from '@axe-core/playwright'
import { paths as routes } from './routes.ts'

const baseUrl = process.env.A11Y_BASE_URL ?? 'http://localhost:4173'
const reportDir = new URL('../a11y-reports/', import.meta.url)


await mkdir(reportDir, { recursive: true })
const browser = await chromium.launch()
const context = await browser.newContext()
const page = await context.newPage()
let failed = false

for (const route of routes) {
  await page.goto(baseUrl + route, { waitUntil: 'networkidle' })
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze()

  const name = route === '/' ? 'home' : route.slice(1).replaceAll('/', '_')
  await writeFile(new URL(`${name}.json`, reportDir), JSON.stringify(results, null, 2))

  if (results.violations.length === 0) {
    console.log(`✔ ${route}`)
    continue
  }
  failed = true
  console.log(`✘ ${route}: ${results.violations.length} violation(s)`)
  for (const v of results.violations) {
    console.log(`  - [${v.impact}] ${v.id}: ${v.help} (${v.nodes.length} element(s))`)
  }
}

await browser.close()
process.exit(failed ? 1 : 0)
