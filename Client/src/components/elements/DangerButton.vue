<script setup lang="ts">
defineProps<{
  type?: 'button' | 'submit' | 'reset'
  disabled?: boolean
}>()
</script>

<template>
  <button
    :type="type || 'button'"
    :disabled="disabled"
    class="inline-flex items-center justify-center gap-2 px-5 py-3 bg-red-600 hover:bg-red-700 disabled:bg-red-300 disabled:cursor-not-allowed text-white font-medium text-sm rounded-xl transition-all focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 shadow-sm cursor-pointer"
  >
    <slot>Verwijder!</slot>
  </button>
</template>
