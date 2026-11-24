<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  id: string
  label: string
  optional?: boolean
  hint?: string
}>()

defineSlots<{
  default(props: { id: string; describedBy?: string }): unknown
}>()

const hintId = computed(() => (props.hint ? `${props.id}-hint` : undefined))
</script>

<template>
  <div>
    <label :for="id" class="block text-sm font-medium">
      {{ label }}
      <span v-if="optional" class="font-normal text-gray-600">(optioneel)</span>
    </label>

    <p v-if="hint" :id="hintId" class="mt-0.5 text-sm text-gray-600">
      {{ hint }}
    </p>

    <slot v-bind="{ id, describedBy: hintId }" />
  </div>
</template>
