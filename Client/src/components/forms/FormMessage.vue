<script setup lang="ts">
import type { FormMessageData } from '@/composables/forms/useFormMessage'

withDefaults(
  defineProps<{
    message: FormMessageData | null
    testid?: string
  }>(),
  { testid: 'message' },
)
</script>

<template>
  <p
    v-if="message"
    :data-testid="testid"
    :role="message.type === 'error' ? 'alert' : 'status'"
    class="rounded-md p-3"
    :class="message.type === 'error' ? 'bg-red-50 text-red-800' : 'bg-green-50 text-green-800'"
  >
    {{ message.text }}
  </p>
</template>
