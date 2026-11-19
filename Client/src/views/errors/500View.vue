<script setup lang="ts">
    import ErrorTemplate from './ErrorTemplate.vue';
</script>

<template>
    <ErrorTemplate>
        <template #title>
            500 - Interne Serverfout
        </template>
        <p>Er is aan onze kant iets misgegaan. We werken aan een oplossing!</p>
    </ErrorTemplate>
</template>