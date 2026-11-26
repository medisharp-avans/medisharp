<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'submit' | 'button' | 'reset'
    loading?: boolean
    loadingLabel?: string
    disabled?: boolean
  }>(),
  {
    type: 'submit',
    loading: false,
    loadingLabel: 'Bezig...',
    disabled: false,
  },
)
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    :aria-busy="loading"
    class="inline-flex min-h-11 items-center justify-center gap-2 rounded-md bg-blue-700 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-blue-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:cursor-not-allowed disabled:bg-slate-400 disabled:text-slate-950"
  >
    <span v-if="$slots.icon" class="shrink-0" aria-hidden="true">
      <slot name="icon" />
    </span>

    <template v-if="loading">
      {{ loadingLabel }}
    </template>
    <slot v-else>Opslaan</slot>
  </button>
</template>
