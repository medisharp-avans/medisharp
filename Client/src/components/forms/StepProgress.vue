<script setup lang="ts">
const props = defineProps<{
  steps: { id: string; title: string }[]
  current: number
}>()

const emit = defineEmits<{ select: [index: number] }>()

function stateOf(index: number) {
  if (index < props.current) return 'complete'
  if (index === props.current) return 'current'
  return 'upcoming'
}

function circleClass(index: number) {
  const state = stateOf(index)
  if (state === 'complete') return 'border-blue-600 bg-blue-600 text-white'
  if (state === 'current') return 'border-blue-600 text-blue-700'
  return 'border-gray-300 text-gray-600'
}
</script>

<template>
  <nav aria-label="Voortgang" data-testid="step-progress">
    <p class="mb-2 text-sm text-gray-600" data-testid="step-progress-label">
      Stap {{ current + 1 }} van {{ steps.length }}
    </p>

    <div class="h-2 w-full overflow-hidden rounded-full bg-gray-200" aria-hidden="true">
      <div
        class="h-full rounded-full bg-blue-600 transition-all duration-300 motion-reduce:transition-none"
        :style="{ width: `${((current + 1) / steps.length) * 100}%` }"
        data-testid="step-progress-bar"
      />
    </div>

    <ol class="mt-3 flex justify-between gap-2">
      <li
        v-for="(step, index) in steps"
        :key="step.id"
        :data-testid="`step-indicator-${step.id}`"
        :data-state="stateOf(index)"
        :aria-current="index === current ? 'step' : undefined"
      >
        <component
          :is="index < current ? 'button' : 'span'"
          v-bind="index < current ? { type: 'button', onClick: () => emit('select', index) } : {}"
          class="flex items-center gap-2 rounded-md text-sm focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600"
        >
          <span
            class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full border text-xs font-semibold"
            :class="circleClass(index)"
            aria-hidden="true"
          >
            {{ index < current ? '✓' : index + 1 }}
          </span>
          <span
            class="sr-only sm:not-sr-only"
            :class="index === current ? 'font-semibold' : 'text-gray-600'"
          >
            {{ step.title }}
          </span>
          <span v-if="index < current" class="sr-only">(afgerond)</span>
        </component>
      </li>
    </ol>
  </nav>
</template>
