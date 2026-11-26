<script setup lang="ts">
// Hoofdlayout: header + zijnavigatie + inhoud. Routes met meta.hideLayout (bv. foutpagina's)
// tonen alleen de inhoud.
import AppHeader from '@/components/Layout/AppHeader.vue'
import AppMain from '@/components/Layout/AppMain.vue'
import AppNavigation from '@/components/Layout/AppNavigation.vue'
import { nextTick, onMounted, onUnmounted, watch } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useKeyboardShortcuts } from '@/composables/general/useKeyboardShortcuts'
import { useMobileNavigation } from '@/composables/general/useMobileNavigation'

const route = useRoute()
const { isOpen: navigationOpen, close: closeNavigation } = useMobileNavigation()

useKeyboardShortcuts()

// Op kleine schermen: na het kiezen van een pagina gaat het menu dicht.
watch(() => route.fullPath, closeNavigation)

// Bij openen gaat de focus naar de eerste link, bij sluiten terug naar de menuknop.
watch(navigationOpen, async (open) => {
  await nextTick()
  const target = open
    ? document.getElementById('main-navigation')?.querySelector<HTMLElement>('a')
    : document.getElementById('menu-button')
  if (target && target.offsetParent !== null) target.focus()
})

// When the screen becomes wide enough for the fixed navigation, the mobile menu state no longer applies.
const largeScreen = window.matchMedia('(min-width: 1024px)')
const closeOnLargeScreen = (event: MediaQueryListEvent) => {
  if (event.matches) closeNavigation()
}
onMounted(() => largeScreen.addEventListener('change', closeOnLargeScreen))
onUnmounted(() => largeScreen.removeEventListener('change', closeOnLargeScreen))

function closeOnEscape(event: KeyboardEvent) {
  if (event.key === 'Escape' && navigationOpen.value) closeNavigation()
}
</script>

<template>
  <a
    href="#main-content"
    class="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:px-4 focus:py-2 focus:bg-white focus:text-gray-900 focus:rounded-xl focus:ring-2 focus:ring-blue-600"
  >
    Ga naar hoofdinhoud
  </a>

  <div v-if="route.meta.hideLayout" class="min-h-screen bg-white">
    <AppMain>
      <RouterView />
    </AppMain>
  </div>

  <div v-else class="min-h-screen flex flex-col bg-white">
    <AppHeader />

    <div class="relative flex flex-1" @keydown="closeOnEscape">
      <!-- Grote schermen: navigatie altijd zichtbaar. Kleine schermen: uitklapmenu via de menuknop. -->
      <div
        v-if="navigationOpen"
        class="fixed inset-x-0 bottom-0 top-18 sm:top-20 z-30 bg-gray-900/40 lg:hidden"
        aria-hidden="true"
        @click="closeNavigation"
      ></div>
      <aside
        id="main-navigation-panel"
        class="bg-gray-100 border-r border-gray-200 lg:static lg:block lg:w-1/8 lg:min-w-62.5 lg:max-w-[25%]"
        :class="
          navigationOpen
            ? 'fixed bottom-0 left-0 top-18 sm:top-20 z-40 block w-72 max-w-[85%] overflow-y-auto shadow-xl'
            : 'hidden'
        "
      >
        <AppNavigation />
      </aside>

      <!-- inert: while the menu is open, keyboard and screen reader users stay inside the menu. -->
      <AppMain :inert="navigationOpen">
        <RouterView />
      </AppMain>
    </div>
  </div>
</template>
