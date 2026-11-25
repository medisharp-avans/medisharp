<script setup lang="ts">
import { computed, ref, watchEffect } from 'vue'

defineOptions({ inheritAttrs: false })

const props = defineProps<{
  id: string
  modelValue: string
  options: { key: string | number; value: string; description?: string }[]
  required?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const inputRef = ref<HTMLInputElement | null>(null)
const listId = computed(() => `${props.id}-options`)

watchEffect(
  () => {
    const text = props.modelValue
    const isOption = props.options.some((option) => option.value === text)
    inputRef.value?.setCustomValidity(
      text !== '' && !isOption ? 'Kies een optie uit de lijst.' : '',
    )
  },
  { flush: 'post' },
)

function onInput(event: Event) {
  emit('update:modelValue', (event.target as HTMLInputElement).value)
}
</script>

<template>
  <div>
    <input
      v-bind="$attrs"
      :id="id"
      ref="inputRef"
      :value="modelValue"
      :list="listId"
      :required="required"
      type="text"
      autocomplete="off"
      class="mt-1 w-full rounded-md border px-3 py-2 disabled:bg-gray-100 disabled:text-gray-500"
      @input="onInput"
    />

    <datalist :id="listId">
      <option v-for="option in options" :key="option.key" :value="option.value">
        {{ option.description }}
      </option>
    </datalist>
  </div>
</template>
