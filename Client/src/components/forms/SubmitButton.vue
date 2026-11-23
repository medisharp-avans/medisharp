<script setup lang="ts">
import { Loading03Icon } from '@hugeicons/core-free-icons'
import { HugeiconsIcon } from '@hugeicons/vue'
import BaseButton from '@/components/elements/BaseButton.vue'

/**
 * Submit button for forms. While loading it is disabled, which prevents a double submit,
 * and it shows a spinner with a loading text.
 *
 * @example
 * <SubmitButton :loading="isSubmitting">Toevoegen</SubmitButton>
 */
withDefaults(
  defineProps<{
    /** True while the form is being submitted. */
    loading?: boolean
    /** Text shown next to the spinner while loading. */
    loadingLabel?: string
  }>(),
  {
    loading: false,
    loadingLabel: 'Opslaan...',
  },
)
</script>

<template>
  <!-- aria-busy tells screen readers that the action is still in progress. -->
  <BaseButton type="submit" :disabled="loading" :aria-busy="loading">
    <template v-if="loading">
      <!-- Decorative only: the loading text already describes the state. -->
      <HugeiconsIcon
        :icon="Loading03Icon"
        :size="16"
        color="currentColor"
        class="animate-spin"
        aria-hidden="true"
      />
      {{ loadingLabel }}
    </template>
    <!-- The normal button text. -->
    <slot v-else></slot>
  </BaseButton>
</template>
