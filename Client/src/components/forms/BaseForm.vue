<script setup lang="ts">
/**
 * Form wrapper that handles submitting without a page reload.
 * `novalidate` turns off the browser's own popups, because useForm handles validation
 * and the fields show the error messages.
 *
 * @example
 * <BaseForm @submit="handleSubmit">...</BaseForm>
 */
const emit = defineEmits<{ submit: [] }>()
</script>

<template>
  <form novalidate class="flex flex-col gap-5" @submit.prevent="emit('submit')">
    <!-- The form fields and buttons. -->
    <slot></slot>
  </form>
</template>
