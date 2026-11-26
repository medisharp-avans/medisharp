<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import StepProgress from './StepProgress.vue'
import PrimaryButton from '../elements/PrimaryButton.vue';

const props = withDefaults(
  defineProps<{
    steps: { id: string; title: string; description?: string }[]
    modelValue: number
    loading?: boolean
    submitLabel?: string
  }>(),
  { loading: false, submitLabel: 'Opslaan' },
)

const emit = defineEmits<{
  'update:modelValue': [index: number]
  submit: []
}>()

const headingRef = ref<HTMLElement | null>(null)

const activeStep = computed(() => props.steps[props.modelValue])
const isFirst = computed(() => props.modelValue === 0)
const isLast = computed(() => props.modelValue === props.steps.length - 1)

function go(index: number) {
  emit('update:modelValue', Math.min(Math.max(index, 0), props.steps.length - 1))
}

// Alleen de velden van de huidige stap staan in de DOM, dus de native
// validatie van de browser controleert precies die stap voordat dit afgaat.
function onSubmit() {
  if (isLast.value) emit('submit')
  else go(props.modelValue + 1)
}

// Zet de focus op de kop van de nieuwe stap, zodat toetsenbord- en
// screenreadergebruikers weten dat er iets veranderd is.
watch(
  () => props.modelValue,
  async () => {
    await nextTick()
    headingRef.value?.focus()
  },
)
</script>

<template>
  <div>
    <StepProgress :steps="steps" :current="modelValue" @select="go" />

    <form class="mt-6 space-y-6" data-testid="step-form" @submit.prevent="onSubmit">
      <section
        v-if="activeStep"
        :aria-labelledby="`${activeStep.id}-heading`"
        :data-testid="`step-${activeStep.id}`"
        class="space-y-5"
      >
        <div>
          <h2
            :id="`${activeStep.id}-heading`"
            ref="headingRef"
            tabindex="-1"
            class="text-xl font-semibold focus:outline-none"
            data-testid="step-title"
          >
            {{ activeStep.title }}
          </h2>
          <p v-if="activeStep.description" class="mt-1 text-sm text-gray-600">
            {{ activeStep.description }}
          </p>
        </div>

        <slot :name="`step-${activeStep.id}`" />
      </section>

      <div class="flex items-center justify-between pt-2">
        <button
          v-if="!isFirst"
          type="button"
          data-testid="step-previous"
          class="rounded-md border px-5 py-2 font-medium hover:bg-gray-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600"
          @click="go(modelValue - 1)"
        >
          Vorige
        </button>
        <span v-else />

         <PrimaryButton
          :data-testid="isLast ? 'submit' : 'step-next'"
          :loading="isLast && loading"
        >
          {{ isLast ? submitLabel : 'Volgende' }}
        </PrimaryButton>
      </div>
    </form>
  </div>
</template>
