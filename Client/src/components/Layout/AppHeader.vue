<script setup lang="ts">
// Bovenbalk met logo/home-link, applicatienaam en profielknop.
import IconLogo from '@/components/icons/IconLogo.vue'
import { RouterLink } from 'vue-router'
import { HugeiconsIcon } from '@hugeicons/vue'
import { UserCircleIcon } from '@hugeicons/core-free-icons'
</script>
<template>
  <header
    class="flex justify-between items-center gap-3 px-4 py-4 bg-gray-100 border-b border-gray-200 text-gray-900 sm:px-6"
  >
    <div class="flex min-w-0 items-center gap-3">
      <RouterLink
        :to="{ name: 'Home' }"
        class="flex shrink-0 items-center justify-center w-12 h-12 rounded-full cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
        aria-label="Home"
        id="home-button"
      >
        <IconLogo class="w-10 h-10 text-gray-900" />
      </RouterLink>
      <h1 class="text-base leading-tight font-semibold sm:text-lg">De Zachte Heelmeesters</h1>
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
