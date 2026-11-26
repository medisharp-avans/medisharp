<script setup lang="ts">
withDefaults(
  defineProps<{
    maxWidth?: 'md' | 'lg' | 'xl' | '2xl' | '4xl'
  }>(),
  {
    maxWidth: '2xl',
  },
)

const widthClasses: Record<string, string> = {
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-xl',
  '2xl': 'max-w-2xl',
  '4xl': 'max-w-4xl',
}
</script>

<template>
  <div class="mx-auto my-6 w-full px-4 sm:my-8 sm:px-6" :class="widthClasses[maxWidth]">
    <section
      class="w-full rounded-xl border border-gray-300 bg-gray-100 p-6 text-gray-900 shadow-xs sm:p-8"
    >
      <div class="mb-5 border-b border-gray-300 pb-4">
        <h2 class="text-lg font-semibold text-gray-900">
          <slot name="title"></slot>
        </h2>
        <p class="mt-1 text-sm text-gray-700">
          <slot name="description"></slot>
        </p>
      </div>

      <div>
        <slot></slot>
      </div>
    </section>
  </div>
</template>
