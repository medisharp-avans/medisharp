<script setup lang="ts" generic="T extends string | number">
// Dropdown with a visible label, for filters and other choices.
// With emptyLabel the list starts with an option for "no choice" (value null), e.g. "Alle statussen".
// Attributes such as data-testid end up on the <select>.
import { HugeiconsIcon } from '@hugeicons/vue'
import { ArrowDown01Icon } from '@hugeicons/core-free-icons'

defineOptions({ inheritAttrs: false })

defineProps<{
  id: string
  label: string
  options: { value: T; label: string }[]
  /** Text of the first option, which sets the value to null. Leave empty to always require a choice. */
  emptyLabel?: string
  /** Keeps the label for screen readers, but hides it on screen. */
  hideLabel?: boolean
}>()

const model = defineModel<T | null>({ required: true })
</script>

<template>
  <div>
    <label
      :for="id"
      class="block text-sm font-medium text-gray-700 mb-1"
      :class="hideLabel ? 'sr-only' : ''"
    >
      {{ label }}
    </label>
    <div class="relative">
      <select
        v-bind="$attrs"
        :id="id"
        v-model="model"
        :name="id"
        class="h-10 w-full appearance-none rounded-xl border border-gray-300 bg-white pl-3 pr-10 text-gray-900 cursor-pointer hover:border-gray-400 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500"
      >
        <option v-if="emptyLabel" :value="null">{{ emptyLabel }}</option>
        <option v-for="option in options" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
      <HugeiconsIcon
        :icon="ArrowDown01Icon"
        class="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500"
        aria-hidden="true"
      />
    </div>
  </div>
</template>
