<script setup lang="ts">
// Algemene knop. variant bepaalt de kleur: primary (hoofdactie), secondary (alternatief),
// danger (verwijderen e.d.). Tekst via de default slot.
const props = withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'danger'
    type?: 'button' | 'submit' | 'reset'
    disabled?: boolean
  }>(),
  { variant: 'primary', type: 'button', disabled: false },
)

const variantClasses = {
  primary:
    'bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white focus-visible:ring-blue-500',
  secondary:
    'bg-white hover:bg-gray-100 disabled:bg-gray-100 disabled:text-gray-400 text-gray-900 border border-gray-200 focus-visible:ring-gray-400',
  danger: 'bg-red-600 hover:bg-red-700 disabled:bg-red-300 text-white focus-visible:ring-red-500',
}
</script>

<template>
  <button
    :type="props.type"
    :disabled="props.disabled"
    :class="[
      'inline-flex items-center justify-center gap-2 px-5 py-3 font-medium text-sm rounded-xl transition-all shadow-sm cursor-pointer disabled:cursor-not-allowed focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
      variantClasses[props.variant],
    ]"
  >
    <slot></slot>
  </button>
</template>
