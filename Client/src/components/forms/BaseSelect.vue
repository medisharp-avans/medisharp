<script setup lang="ts" generic="T extends string | number">
import { computed } from 'vue'
import { ArrowDown01Icon } from '@hugeicons/core-free-icons'
import { HugeiconsIcon } from '@hugeicons/vue'
import BaseField from './BaseField.vue'

/**
 * Dropdown for use with v-model.
 * Thanks to v-model a numeric option stays a number instead of becoming a string.
 *
 * @example
 * <BaseSelect
 *   v-model="values.ruleId"
 *   label="Regel"
 *   placeholder="Kies een regel"
 *   :options="[{ value: 1, label: 'Consult' }]"
 *   :error="errors.ruleId"
 * />
 */

// Extra attributes (disabled, name, ...) belong on the <select>, not on the outer wrapper.
defineOptions({ inheritAttrs: false })

const props = defineProps<{
  /** Visible text above the dropdown. */
  label: string
  /** Plain values, or objects when the visible text differs from the value. */
  options: readonly (T | { value: T; label: string })[]
  /** HTML id of the select. Generated automatically when left out. */
  id?: string
  /** Validation message shown below the dropdown. */
  error?: string
  /** First line while nothing is chosen. It cannot be selected itself. */
  placeholder?: string
}>()

// The chosen value. An empty string means that nothing has been chosen yet.
const model = defineModel<T | ''>({ required: true })

// Gives every option the same { value, label } shape, so the template stays simple.
const normalizedOptions = computed(() =>
  props.options.map((option) =>
    typeof option === 'object' ? option : { value: option, label: String(option) },
  ),
)
</script>

<template>
  <BaseField :id="id" v-slot="{ controlAttrs, controlClass }" :label="label" :error="error">
    <!-- Relative wrapper, so the arrow icon can be positioned on top of the select. -->
    <div class="relative">
      <!-- appearance-none hides the browser's arrow; the text is muted until a choice is made. -->
      <select
        v-model="model"
        v-bind="{ ...controlAttrs, ...$attrs }"
        :class="[
          controlClass,
          'cursor-pointer appearance-none py-2.5 pl-3.5 pr-10',
          model === '' ? 'text-gray-600' : 'text-gray-900',
        ]"
      >
        <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
        <option v-for="option in normalizedOptions" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
      <!-- pointer-events-none lets clicks on the icon still open the select. -->
      <HugeiconsIcon
        :icon="ArrowDown01Icon"
        :size="16"
        color="currentColor"
        class="pointer-events-none absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-600"
        aria-hidden="true"
      />
    </div>
  </BaseField>
</template>
