<script setup lang="ts">
// Search box with a magnifier icon and a visible label.
// Attributes such as data-testid end up on the <input>.
import { HugeiconsIcon } from '@hugeicons/vue'
import { Search01Icon } from '@hugeicons/core-free-icons'

defineOptions({ inheritAttrs: false })

withDefaults(
  defineProps<{
    id: string
    label?: string
    placeholder?: string
  }>(),
  { label: 'Zoeken', placeholder: '' },
)

const model = defineModel<string>({ required: true })
</script>

<template>
  <div>
    <label :for="id" class="block text-sm font-medium text-gray-700 mb-1">{{ label }}</label>
    <div class="relative">
      <HugeiconsIcon
        :icon="Search01Icon"
        class="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500"
        aria-hidden="true"
      />
      <input
        v-bind="$attrs"
        :id="id"
        v-model="model"
        :name="id"
        type="search"
        :placeholder="placeholder"
        autocomplete="off"
        class="h-10 w-full rounded-xl border border-gray-300 bg-white pl-10 pr-3 placeholder:text-gray-500 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600"
      />
    </div>
  </div>
</template>
