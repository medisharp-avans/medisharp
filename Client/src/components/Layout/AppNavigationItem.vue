<script setup lang="ts">
    import type { Component } from 'vue';
    import { RouterLink } from 'vue-router';

    defineProps({
        defineRoute: {
            type: String,
            required: true
        },
        icon: {
            type: Object as () => Component,
            required: true
        }
    });
</script>

<template>
    <li>
        <RouterLink 
            :to="{ name: defineRoute }" 
            class="flex items-center gap-3 px-4 py-3 rounded-xl text-[#111827] hover:bg-gray-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 focus-visible:ring-offset-2 transition-all font-medium"
            active-class="!bg-blue-100 hover:!bg-blue-200 !text-blue-900 font-semibold shadow-sm"
            :id="defineRoute + 'Route-link'"
        >
            <component :is="icon" class="w-6 h-6" />
            <slot></slot>
        </RouterLink>
    </li>
</template>