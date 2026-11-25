<script setup lang="ts" generic="T extends string | number">
import FormField from './FormField.vue'

// Attributen zoals data-testid en disabled komen op de <select> terecht.
defineOptions({ inheritAttrs: false })

const props = defineProps<{
  id: string
  label: string
  modelValue: T | ''
  options: { value: T; label: string }[]
  placeholder?: string
  hint?: string
  optional?: boolean
  required?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: T | ''] }>()

function onChange(event: Event) {
  const raw = (event.target as HTMLSelectElement).value
  const match = props.options.find((option) => String(option.value) === raw)
  emit('update:modelValue', match ? match.value : '')
}
</script>

<template>
  <FormField
    :id="id"
    :label="label"
    :hint="hint"
    :optional="optional"
    v-slot="{ id: fieldId, describedBy }"
  >
    <select
      v-bind="$attrs"
      :id="fieldId"
      :value="modelValue"
      :required="required"
      :aria-describedby="describedBy"
      class="mt-1 w-full rounded-md border px-3 py-2 disabled:bg-gray-100 disabled:text-gray-500"
      @change="onChange"
    >
      <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
      <option v-for="option in options" :key="option.value" :value="option.value">
        {{ option.label }}
      </option>
    </select>
  </FormField>
</template>
