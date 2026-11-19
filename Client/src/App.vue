<script setup lang="ts">
// Hoofdlayout: header + zijnavigatie + inhoud. Routes met meta.hideLayout (bv. foutpagina's)
// tonen alleen de inhoud.
import AppHeader from '@/components/Layout/AppHeader.vue'
import AppMain from '@/components/Layout/AppMain.vue'
import AppNavigation from '@/components/Layout/AppNavigation.vue'
import { RouterView, useRoute } from 'vue-router'
import { useKeyboardShortcuts } from '@/composables/useKeyboardShortcuts'

const route = useRoute()

useKeyboardShortcuts()
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

    <div class="flex flex-1">
      <aside class="w-1/8 min-w-62.5 max-w-[25%] bg-gray-100 border-r border-gray-200">
        <AppNavigation />
      </aside>

      <AppMain>
        <RouterView />
      </AppMain>
    </div>
  </div>
</template>
