import { onMounted, onUnmounted } from 'vue'

/** 
 * A lookup record mapping shortcut keys to functions that resolve the target HTMLElement.
 */
const shortcutTargets: Record<string, () => HTMLElement | null | undefined> = {
  n: () => document.getElementById('main-navigation')?.querySelector<HTMLElement>('a, button'),
  p: () => document.getElementById('profile-button'),
  m: () => document.getElementById('main-content'),
}

/**
 * Handles a keydown event for keyboard shortcuts.
 * 
 * Listens for the `Alt` key combined with `N`, `P`, or `M` to shift focus to 
 * the navigation, profile button, or main content respectively. Other keys are ignored.
 * 
 * @param event - The KeyboardEvent triggered by the user input.
 */
export function handleKeyboardShortcut(event: KeyboardEvent): void {
  if (!event.altKey) return

  const getTarget = shortcutTargets[event.key.toLowerCase()]
  if (!getTarget) return

  event.preventDefault()
  getTarget()?.focus()
}

/**
 * Automatically registers and cleans up global keyboard shortcuts while the 
 * calling component is mounted.
 * 
 * Designed to be called inside a `<script setup>` block.
 */
export function useKeyboardShortcuts(): void {
  onMounted(() => window.addEventListener('keydown', handleKeyboardShortcut))
  onUnmounted(() => window.removeEventListener('keydown', handleKeyboardShortcut))
}
