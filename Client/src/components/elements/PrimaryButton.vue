<script setup lang="ts">
    defineProps<{
        type?: 'button' | 'submit' | 'reset';
        disabled?: boolean;
    }>();
</script>

<template>
    <button
        :type="type || 'button'"
        :disabled="disabled"
        class="inline-flex items-center justify-center gap-2 px-5 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 disabled:cursor-not-allowed text-white font-medium text-sm rounded-xl transition-all focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 shadow-sm cursor-pointer"
    >
        <slot>Klik hier!</slot>
    </button>
</template>