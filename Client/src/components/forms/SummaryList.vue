<script setup lang="ts">
defineProps<{
  items: {
    testid: string
    label: string
    value: string
    step?: number
  }[]
}>()

const emit = defineEmits<{ edit: [step: number] }>()
</script>

<template>
  <dl class="divide-y rounded-md border" data-testid="summary">
    <div
      v-for="item in items"
      :key="item.testid"
      class="flex items-start justify-between gap-4 p-3"
    >
      <div class="min-w-0">
        <dt class="text-sm text-gray-600">{{ item.label }}</dt>
        <dd
          class="mt-0.5 whitespace-pre-line wrap-break-word font-medium"
          :data-testid="`summary-${item.testid}`"
        >
          {{ item.value || '—' }}
        </dd>
      </div>

      <button
        v-if="item.step !== undefined"
        type="button"
        class="shrink-0 rounded-md text-sm text-blue-700 underline hover:text-blue-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600"
        :data-testid="`summary-edit-${item.testid}`"
        @click="emit('edit', item.step)"
      >
        Wijzig<span class="sr-only"> {{ item.label.toLowerCase() }}</span>
      </button>
    </div>
  </dl>
</template>
