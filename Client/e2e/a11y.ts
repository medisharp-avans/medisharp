// Runs axe-core accessibility checks (WCAG 2.2 AA) against every route of the built app.
// Starts its own `vite preview` server, so run `npm run build` first.
// Writes a JSON report per route to ./a11y-reports and exits 1 on any violation.
import { mkdir, writeFile } from 'node:fs/promises'
import { preview } from 'vite'
import { chromium } from 'playwright'
import { AxeBuilder } from '@axe-core/playwright'
import { baseUrl, paths, previewPort } from './routes.ts'

const reportDir = new URL('../a11y-reports/', import.meta.url)

await mkdir(reportDir, { recursive: true })
const server = await preview({ preview: { port: previewPort, strictPort: true } })
const browser = await chromium.launch()
const page = await (await browser.newContext()).newPage()
let hasViolations = false

try {
  for (const path of paths) {
    await page.goto(baseUrl + path, { waitUntil: 'networkidle' })
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
      .analyze()

    const reportName = path === '/' ? 'home' : path.slice(1).replaceAll('/', '_')
    await writeFile(new URL(`${reportName}.json`, reportDir), JSON.stringify(results, null, 2))

    if (results.violations.length === 0) {
      console.log(`✔ ${path}`)
      continue
    }
    hasViolations = true
    console.log(`✘ ${path}: ${results.violations.length} violation(s)`)
    for (const violation of results.violations) {
      console.log(
        `  - [${violation.impact}] ${violation.id}: ${violation.help} (${violation.nodes.length} element(s))`,
      )
    }
  }
} finally {
  await browser.close()
  await server.close()
}

process.exit(hasViolations ? 1 : 0)
