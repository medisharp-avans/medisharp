<script setup>
    import ErrorTemplate from './ErrorTemplate.vue';
</script>

<template>
    <ErrorTemplate>
        <template #title>
            400 - Slechte request
        </template>
        <p>De server kan de aanvraag niet verwerken door een clientfout!</p>
    </ErrorTemplate>
</template>