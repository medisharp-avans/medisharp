<script setup lang="ts">
import FormField from './FormField.vue'

// Attributen zoals data-testid komen op de <textarea> terecht.
defineOptions({ inheritAttrs: false })

withDefaults(
  defineProps<{
    id: string
    label: string
    modelValue: string
    hint?: string
    optional?: boolean
    required?: boolean
    rows?: number
    maxlength?: number
  }>(),
  { rows: 3 },
)

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
    <textarea
      v-bind="$attrs"
      :id="fieldId"
      :value="modelValue"
      :rows="rows"
      :maxlength="maxlength"
      :required="required"
      :aria-describedby="describedBy"
      class="mt-1 w-full rounded-md border px-3 py-2"
      @input="emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
    />
  </FormField>
</template>
