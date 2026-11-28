<script setup lang="ts">
// Invoerveld met label en optionele foutmelding. Waarde via v-model.
import { useId } from 'vue'

const model = defineModel<string>({ default: '' })

const props = withDefaults(
  defineProps<{
    label: string
    type?: 'text' | 'email' | 'password' | 'tel'
    autocomplete?: string
    required?: boolean
    disabled?: boolean
    error?: string | null
  }>(),
  { type: 'text', autocomplete: 'off', required: false, disabled: false, error: null },
)

const id = useId()
const errorId = `${id}-error`
</script>

<template>
  <div class="flex flex-col gap-1.5">
    <label :for="id" class="text-sm font-medium text-gray-900">
      {{ props.label }}
      <span v-if="props.required" class="text-red-600" aria-hidden="true">*</span>
    </label>
    <input
      :id="id"
      v-model="model"
      :type="props.type"
      :autocomplete="props.autocomplete"
      :required="props.required"
      :disabled="props.disabled"
      :aria-invalid="props.error ? 'true' : undefined"
      :aria-describedby="props.error ? errorId : undefined"
      :class="[
        'w-full px-4 py-3 text-sm rounded-xl border bg-white text-gray-900 placeholder-gray-400 transition-all focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:bg-gray-100 disabled:text-gray-400',
        props.error
          ? 'border-red-500 focus-visible:ring-red-500'
          : 'border-gray-200 focus-visible:ring-blue-500',
      ]"
    />
    <p v-if="props.error" :id="errorId" class="text-sm text-red-600">{{ props.error }}</p>
  </div>
</template>
