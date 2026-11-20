<script setup lang="ts">
// Gedeeld invoerveld voor de verwijzing, met een gekoppeld label en eventuele toelichting.
const model = defineModel<string>({ required: true })

const props = withDefaults(
  defineProps<{
    id: string
    label: string
    hint?: string
    type?: 'text' | 'email' | 'tel'
    inputmode?: 'text' | 'numeric' | 'email' | 'tel'
    maxlength?: number
    multiline?: boolean
    rows?: number
  }>(),
  { type: 'text', multiline: false, rows: 3 },
)

const controlClasses =
  'block w-full min-w-0 rounded-xl border border-gray-500 bg-white px-3 py-2.5 text-base leading-6 text-gray-900 shadow-sm transition-colors hover:border-gray-600 focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-600'
</script>

<template>
  <div class="min-w-0">
    <label :for="props.id" class="mb-2 block text-sm font-medium text-gray-900">
      {{ props.label }}
    </label>
    <textarea
      v-if="props.multiline"
      :id="props.id"
      v-model="model"
      :name="props.id"
      :rows="props.rows"
      :maxlength="props.maxlength"
      :aria-describedby="props.hint ? `${props.id}-hint` : undefined"
      :class="[controlClasses, 'resize-y']"
      autocomplete="off"
      required
    />
    <input
      v-else
      :id="props.id"
      v-model="model"
      :name="props.id"
      :type="props.type"
      :inputmode="props.inputmode"
      :maxlength="props.maxlength"
      :aria-describedby="props.hint ? `${props.id}-hint` : undefined"
      :class="controlClasses"
      autocomplete="off"
      required
    />
    <p v-if="props.hint" :id="`${props.id}-hint`" class="mt-2 text-sm leading-5 text-gray-600">
      {{ props.hint }}
    </p>
  </div>
</template>
