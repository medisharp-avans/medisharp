import { onMounted, onUnmounted } from 'vue'

// Per sneltoets (Alt + letter) het element dat focus moet krijgen.
const shortcutTargets: Record<string, () => HTMLElement | null | undefined> = {
  n: () => document.getElementById('main-navigation')?.querySelector<HTMLElement>('a, button'),
  p: () => document.getElementById('profile-button'),
  m: () => document.getElementById('main-content'),
}

/**
 * Verwerkt een keydown-event: bij Alt + N/P/M krijgt respectievelijk de navigatie,
 * de profielknop of de hoofdinhoud focus. Andere toetsen worden genegeerd.
 */
export function handleKeyboardShortcut(event: KeyboardEvent): void {
  if (!event.altKey) return

  const getTarget = shortcutTargets[event.key.toLowerCase()]
  if (!getTarget) return

  event.preventDefault()
  getTarget()?.focus()
}

/**
 * Registreert de toetsenbord-sneltoetsen zolang het aanroepende component gemount is.
 * Roep aan vanuit <script setup>.
 */
export function useKeyboardShortcuts(): void {
  onMounted(() => window.addEventListener('keydown', handleKeyboardShortcut))
  onUnmounted(() => window.removeEventListener('keydown', handleKeyboardShortcut))
}
