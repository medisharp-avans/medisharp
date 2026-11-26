<script setup lang="ts">
// Bovenbalk met menuknop (alleen op kleine schermen), logo/home-link, applicatienaam en profielknop.
import IconLogo from '@/components/icons/IconLogo.vue'
import { RouterLink } from 'vue-router'
import { HugeiconsIcon } from '@hugeicons/vue'
import { Cancel01Icon, Menu01Icon, UserCircleIcon } from '@hugeicons/core-free-icons'
import { useMobileNavigation } from '@/composables/general/useMobileNavigation'

const { isOpen, toggle } = useMobileNavigation()
</script>
<template>
  <header
    class="flex justify-between items-center gap-2 px-3 py-3 sm:px-6 sm:py-4 bg-gray-100 border-b border-gray-200 text-gray-900"
  >
    <div class="flex min-w-0 items-center gap-1 sm:gap-3">
      <button
        type="button"
        class="flex lg:hidden items-center justify-center w-12 h-12 shrink-0 rounded-full hover:bg-gray-200 cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        :aria-expanded="isOpen"
        aria-controls="main-navigation-panel"
        :aria-label="isOpen ? 'Menu sluiten' : 'Menu openen'"
        id="menu-button"
        @click="toggle"
      >
        <HugeiconsIcon
          :icon="isOpen ? Cancel01Icon : Menu01Icon"
          class="w-7 h-7 text-gray-900"
          aria-hidden="true"
        />
      </button>
      <RouterLink
        :to="{ name: 'Home' }"
        class="flex items-center justify-center w-12 h-12 shrink-0 rounded-full cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        aria-label="Home"
        id="home-button"
      >
        <IconLogo class="w-10 h-10 text-gray-900" />
      </RouterLink>
      <!-- Geen h1: elke pagina heeft zijn eigen h1 als hoofdkop. -->
      <p class="truncate text-base sm:text-lg font-semibold">De Zachte Heelmeesters</p>
    </div>

    <div class="shrink-0">
      <RouterLink
        :to="{ name: 'Profile' }"
        class="flex items-center justify-center w-12 h-12 rounded-full hover:bg-gray-200 transition-colors cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        aria-label="Profiel"
        id="profile-button"
      >
        <HugeiconsIcon :icon="UserCircleIcon" class="w-8 h-8 text-gray-900" aria-hidden="true" />
      </RouterLink>
    </div>
  </header>
</template>
