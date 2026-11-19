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
    class="inline-flex items-center justify-center gap-2 px-5 py-3 bg-white hover:bg-[#F3F4F6] disabled:bg-gray-100 disabled:text-gray-400 disabled:cursor-not-allowed text-[#111827] border border-[#E5E7EB] font-medium text-sm rounded-xl transition-all focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-offset-2 shadow-sm cursor-pointer"
  >
    <slot>Klik mij als tweede optie!</slot>
  </button>
</template>
