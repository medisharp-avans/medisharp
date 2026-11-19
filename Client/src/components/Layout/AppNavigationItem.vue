<script setup lang="ts">
import type { PropType } from 'vue'
import { RouterLink } from 'vue-router'
import { HugeiconsIcon } from '@hugeicons/vue'
import type { Home01Icon } from '@hugeicons/core-free-icons'

// Alle Hugeicons hebben hetzelfde type; de package exporteert dat type niet los.
type IconSvgObject = typeof Home01Icon

defineProps({
  defineRoute: {
    type: String,
    required: true,
  },
  icon: {
    type: Array as PropType<IconSvgObject>,
    required: true,
  },
})
</script>

<template>
  <li>
    <RouterLink
      :to="{ name: defineRoute }"
      class="flex items-center gap-3 px-4 py-3 rounded-xl text-[#111827] hover:bg-gray-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2 transition-all font-medium"
      active-class="!bg-blue-100 hover:!bg-blue-200 !text-blue-900 font-semibold shadow-sm"
      :id="defineRoute + 'Route-link'"
    >
      <HugeiconsIcon :icon="icon" class="w-6 h-6" aria-hidden="true" />
      <slot></slot>
    </RouterLink>
  </li>
</template>
