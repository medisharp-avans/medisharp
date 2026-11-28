<script setup lang="ts">
import FormField from './FormField.vue'
import DatalistInput from './DataListInput.vue'

// Attributen zoals data-testid, placeholder en disabled komen op het <input> terecht.
defineOptions({ inheritAttrs: false })

defineProps<{
  id: string
  label: string
  modelValue: string
  options: { key: string | number; value: string; description?: string }[]
  hint?: string
  optional?: boolean
  required?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
</script>

<template>
  <FormField
    :id="id"
    :label="label"
    :hint="hint"
    :optional="optional"
    v-slot="{ id: fieldId, describedBy }"
  >
    <DatalistInput
      v-bind="$attrs"
      :id="fieldId"
      :model-value="modelValue"
      :options="options"
      :required="required"
      :aria-describedby="describedBy"
      @update:model-value="emit('update:modelValue', $event)"
    />
  </FormField>
</template>
