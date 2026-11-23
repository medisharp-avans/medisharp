<script setup lang="ts">
import { computed } from 'vue'
import BaseField from './BaseField.vue'

/**
 * Multi-line text field for use with v-model.
 * With `maxlength` it shows a counter, so the user can see how much room is left.
 *
 * @example
 * <BaseTextarea v-model="values.note" label="Notitie" :maxlength="200" :error="errors.note" />
 */

// Extra attributes (rows, disabled, ...) belong on the <textarea>, not on the outer wrapper.
defineOptions({ inheritAttrs: false })

const props = defineProps<{
  /** Visible text above the field. Also the placeholder when none is given. */
  label: string
  /** HTML id of the textarea. Generated automatically when left out. */
  id?: string
  /** Validation message shown below the field. */
  error?: string
  /** Example text shown while the field is empty. */
  placeholder?: string
  /** Maximum number of characters. Also turns on the character counter. */
  maxlength?: number
}>()

// The text of the field, connected to the parent through v-model.
const model = defineModel<string>({ required: true })

// Passed to BaseField as hint, which also links it to the textarea for screen readers.
const characterCount = computed(() =>
  props.maxlength ? `${model.value.length} / ${props.maxlength} tekens` : undefined,
)
</script>

<template>
  <BaseField
    :id="id"
    v-slot="{ controlAttrs, controlClass }"
    :label="label"
    :error="error"
    :hint="characterCount"
  >
    <!-- $attrs comes last, so the parent can override defaults such as rows. -->
    <textarea
      v-model="model"
      rows="4"
      :placeholder="placeholder || label"
      :maxlength="maxlength"
      v-bind="{ ...controlAttrs, ...$attrs }"
      :class="[controlClass, 'px-3.5 py-2.5 text-gray-900 placeholder:text-gray-600']"
    ></textarea>
  </BaseField>
</template>
