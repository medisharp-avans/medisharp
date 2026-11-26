import { ref } from 'vue'

/**
 * Whether the side navigation is open on small screens. Shared between the menu button in the header
 * and the navigation panel in App.vue. On large screens the navigation is always visible.
 */
const isOpen = ref(false)

export function useMobileNavigation() {
  const open = () => (isOpen.value = true)
  const close = () => (isOpen.value = false)
  const toggle = () => (isOpen.value = !isOpen.value)

  return { isOpen, open, close, toggle }
}
