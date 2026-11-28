<script setup lang="ts">
// Eén link in de zijnavigatie: icoon + label (slot). Actief wanneer de route overeenkomt.
import { RouterLink } from 'vue-router'
import { HugeiconsIcon } from '@hugeicons/vue'
import type { Home01Icon } from '@hugeicons/core-free-icons'

// Alle Hugeicons hebben hetzelfde type; de package exporteert dat type niet los.
type IconSvgObject = typeof Home01Icon

defineProps<{
  routeName: string
  icon: IconSvgObject
}>()
</script>

<template>
  <li>
    <RouterLink
      :to="{ name: routeName }"
      class="flex h-full min-w-0 items-center gap-2 px-2 py-3 rounded-xl text-sm text-gray-900 hover:bg-gray-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2 transition-all font-medium md:gap-3 md:px-4 md:text-base"
      active-class="!bg-blue-100 hover:!bg-blue-200 !text-blue-900 font-semibold shadow-sm"
      :id="routeName + 'Route-link'"
    >
      <HugeiconsIcon :icon="icon" class="w-6 h-6 shrink-0" aria-hidden="true" />
      <span class="min-w-0 break-words"><slot></slot></span>
    </RouterLink>
  </li>
</template>
