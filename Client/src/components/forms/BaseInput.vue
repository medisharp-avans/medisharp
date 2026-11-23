<script setup lang="ts" generic="T extends string | number">
import BaseField from './BaseField.vue'

/**
 * Single-line input for use with v-model.
 * The generic `T` keeps the type of the bound value (string or number).
 *
 * @example
 * <BaseInput v-model="values.email" label="E-mail" type="email" :error="errors.email" />
 */

// Extra attributes (type, autocomplete, ...) belong on the <input>, not on the outer wrapper.
defineOptions({ inheritAttrs: false })

defineProps<{
  /** Visible text above the input. Also the placeholder when none is given. */
  label: string
  /** HTML id of the input. Generated automatically when left out. */
  id?: string
  /** Validation message shown below the input. */
  error?: string
  /** Example text shown while the input is empty. */
  placeholder?: string
}>()

// The value of the input, connected to the parent through v-model.
const model = defineModel<T>({ required: true })
</script>

<template>
  <BaseField :id="id" v-slot="{ controlAttrs, controlClass }" :label="label" :error="error">
    <!-- $attrs comes last, so the parent can override the default attributes. -->
    <input
      v-model="model"
      :placeholder="placeholder || label"
      v-bind="{ ...controlAttrs, ...$attrs }"
      :class="[controlClass, 'px-3.5 py-2.5 text-gray-900 placeholder:text-gray-600']"
    />
  </BaseField>
</template>
