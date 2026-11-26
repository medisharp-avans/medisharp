<script setup lang="ts">
// Bar above a list that shows how many rows are selected, with the actions for the selection
// in the default slot. Grey and quiet when nothing is selected, blue as soon as something is.
import { computed } from 'vue'
import { HugeiconsIcon } from '@hugeicons/vue'
import { CheckmarkSquare02Icon } from '@hugeicons/core-free-icons'

const props = withDefaults(
  defineProps<{
    count: number
    /** Name of one selected item, e.g. "declaratie". */
    singular?: string
    /** Name of several selected items, e.g. "declaraties". */
    plural?: string
  }>(),
  { singular: 'item', plural: 'items' },
)

const hasSelection = computed(() => props.count > 0)
</script>

<template>
  <div
    class="mb-3 flex flex-wrap items-center justify-between gap-3 rounded-2xl border px-4 py-3 transition-colors"
    :class="hasSelection ? 'border-blue-200 bg-blue-50' : 'border-gray-200 bg-white'"
  >
    <p
      class="inline-flex items-center gap-2 text-sm"
      :class="hasSelection ? 'text-blue-950' : 'text-gray-600'"
      aria-live="polite"
      data-testid="selection-count"
    >
      <span
        v-if="hasSelection"
        class="inline-flex h-6 min-w-6 items-center justify-center rounded-full bg-blue-600 px-2 text-xs font-bold text-white tabular-nums"
        aria-hidden="true"
        data-testid="selection-badge"
      >
        {{ count }}
      </span>
      <HugeiconsIcon
        v-else
        :icon="CheckmarkSquare02Icon"
        class="w-5 h-5 text-gray-500"
        aria-hidden="true"
      />
      <span v-if="hasSelection" class="font-medium">
        <!-- The badge is hidden for screen readers, so they hear the number here once. -->
        <span class="sr-only">{{ `${count} ` }}</span
        >{{ count === 1 ? singular : plural }} geselecteerd
      </span>
      <span v-else>Selecteer {{ plural }} met de vakjes in de tabel</span>
    </p>

    <div class="flex flex-wrap items-center gap-2">
      <slot />
    </div>
  </div>
</template>
