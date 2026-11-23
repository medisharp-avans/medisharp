<script setup lang="ts">
import { computed, useId } from 'vue'

/**
 * Shared layout of every form field: label, error message, hint and screen reader links.
 * BaseInput, BaseSelect and BaseTextarea only provide the actual control through the slot.
 * The slot receives `controlAttrs` and `controlClass`, which the control must apply.
 */
const props = defineProps<{
  /** Visible text above the control. */
  label: string
  /** HTML id of the control. Generated automatically when left out. */
  id?: string
  /** Validation message. When set, the field is shown as invalid. */
  error?: string
  /** Small extra text below the control, such as a character counter. */
  hint?: string
}>()

// useId gives a stable, unique id, so a label always points to the right control.
const generatedId = useId()
const fieldId = computed(() => props.id ?? generatedId)
const errorId = computed(() => `${fieldId.value}-error`)
const hintId = computed(() => `${fieldId.value}-hint`)

// Accessibility attributes: screen readers announce the error and hint with the control.
const controlAttrs = computed(() => {
  // Only refer to elements that are actually rendered.
  const descriptionIds = [props.error && errorId.value, props.hint && hintId.value]

  return {
    id: fieldId.value,
    'aria-invalid': Boolean(props.error),
    'aria-describedby': descriptionIds.filter(Boolean).join(' ') || undefined,
  }
})

// Styling shared by all controls, with different colors for the error state.
const controlClass = computed(() => [
  'w-full rounded-lg border text-sm outline-none transition-colors focus:bg-white',
  'disabled:cursor-not-allowed disabled:opacity-60',
  props.error
    ? 'border-red-400 bg-red-50 focus:border-red-600'
    : 'border-gray-200 bg-gray-100 focus:border-blue-500',
])
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label :for="fieldId" class="text-xs font-medium text-gray-700">{{ label }}</label>

    <!-- The actual control (input, select or textarea). -->
    <slot :control-attrs="controlAttrs" :control-class="controlClass"></slot>

    <!-- The error stays on the left and the hint on the right, also when only one is shown. -->
    <div v-if="error || hint" class="flex justify-between gap-2">
      <p v-if="error" :id="errorId" class="text-xs text-red-700">{{ error }}</p>
      <p v-if="hint" :id="hintId" class="ml-auto text-xs text-gray-600">{{ hint }}</p>
    </div>
  </div>
</template>
