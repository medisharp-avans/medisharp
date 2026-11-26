<script setup lang="ts">
// Table header cell with a sort button. The arrow shows whether this column is sorted, and in which direction.
// Attributes such as data-testid end up on the button.
import { computed } from 'vue'
import { HugeiconsIcon } from '@hugeicons/vue'
import { ArrowDown02Icon, ArrowUp02Icon, ArrowUpDownIcon } from '@hugeicons/core-free-icons'

defineOptions({ inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    label: string
    /** True when the table is sorted on this column. */
    active: boolean
    descending?: boolean
    alignRight?: boolean
  }>(),
  { descending: false, alignRight: false },
)

const emit = defineEmits<{ sort: [] }>()

const ariaSort = computed(() => {
  if (!props.active) return 'none'
  return props.descending ? 'descending' : 'ascending'
})

const icon = computed(() => {
  if (!props.active) return ArrowUpDownIcon
  return props.descending ? ArrowDown02Icon : ArrowUp02Icon
})
</script>

<template>
  <th
    scope="col"
    class="px-4 py-3 font-semibold whitespace-nowrap"
    :class="alignRight ? 'text-right' : ''"
    :aria-sort="ariaSort"
  >
    <button
      v-bind="$attrs"
      type="button"
      class="inline-flex items-center gap-1 cursor-pointer hover:text-gray-900 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 rounded"
      :class="alignRight ? 'flex-row-reverse' : ''"
      @click="emit('sort')"
    >
      {{ label }}
      <HugeiconsIcon
        :icon="icon"
        class="w-4 h-4"
        :class="active ? 'text-blue-700' : 'text-gray-400'"
        aria-hidden="true"
      />
    </button>
  </th>
</template>
