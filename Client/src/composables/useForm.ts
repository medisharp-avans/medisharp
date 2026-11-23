import { ref, toRaw, type Ref } from 'vue'

/**
 * Checks a single field value.
 * Returns `true` when the value is valid, otherwise the error message to show to the user.
 */
export type ValidationRule<T> = (value: T) => true | string

/**
 * Validation rules per form field. Every rule receives the type of its own field,
 * so a misspelled field name or a rule for the wrong type is a compile error.
 */
export type FormRules<Values> = { [Field in keyof Values]?: ValidationRule<Values[Field]> }

/** Current error messages per field. A field without an error has no entry. */
export type FormErrors<Values> = Partial<Record<keyof Values, string>>

export interface UseFormOptions<Values extends object> {
  /** The starting values of the form. `resetForm` returns to these values. */
  initialValues: Values
  /** Optional validation rules. Fields without a rule are always valid. */
  rules?: FormRules<Values>
  /**
   * Called with a copy of the values once all rules pass. The form resets afterwards,
   * unless this returns `false` (for example when saving failed) to keep the user's input.
   */
  onSubmit: (values: Values) => void | false | Promise<void | false>
}

/**
 * Creates a deep copy, so editing the form can never change the original values.
 * `toRaw` is needed because `structuredClone` cannot copy Vue's reactive proxies.
 */
function cloneValues<Values extends object>(values: Values): Values {
  return structuredClone(toRaw(values))
}

/**
 * Manages the values, validation and submission of a form.
 *
 * @example
 * const { values, errors, isSubmitting, handleSubmit } = useForm({
 *   initialValues: { name: '' },
 *   rules: { name: (value) => !!value.trim() || 'Naam is verplicht.' },
 *   onSubmit: (form) => saveName(form.name),
 * })
 */
export function useForm<Values extends object>({
  initialValues,
  rules = {},
  onSubmit,
}: UseFormOptions<Values>) {
  // Stored separately, so a later change to the passed object cannot affect a reset.
  const initialSnapshot = cloneValues(initialValues)

  // The casts keep the `Values` type: `ref` would otherwise unwrap it into a different type.
  const values = ref(cloneValues(initialValues)) as Ref<Values>
  const errors = ref({}) as Ref<FormErrors<Values>>
  const isSubmitting = ref(false)

  /**
   * Runs the rule of one field and adds its message to `foundErrors` when it fails.
   * The generic `Field` tells TypeScript that the rule and the value belong to the same field.
   */
  function validateField<Field extends keyof Values>(
    field: Field,
    foundErrors: FormErrors<Values>,
  ): void {
    const result = rules[field]?.(values.value[field])
    if (typeof result === 'string') foundErrors[field] = result
  }

  /**
   * Validates all fields that have a rule and returns whether the form is valid.
   * The errors are rebuilt on every run, so messages of fixed fields disappear.
   */
  function validate(): boolean {
    const foundErrors: FormErrors<Values> = {}
    const fields = Object.keys(rules) as (keyof Values)[]
    fields.forEach((field) => validateField(field, foundErrors))

    errors.value = foundErrors
    return fields.every((field) => foundErrors[field] === undefined)
  }

  /** Fills the form with new values, such as the item being edited, and clears all errors. */
  function setValues(newValues: Values): void {
    values.value = cloneValues(newValues)
    errors.value = {}
  }

  /** Puts the form back to its initial values and clears all errors. */
  function resetForm(): void {
    setValues(initialSnapshot)
  }

  /** Validates the form and, when valid, calls `onSubmit`. Connect this to the form's submit. */
  async function handleSubmit(): Promise<void> {
    // A second click while saving must not submit the form twice.
    if (isSubmitting.value || !validate()) return

    isSubmitting.value = true
    try {
      // Pass a copy, so typing during a slow request cannot change the data being saved.
      const result = await onSubmit(cloneValues(values.value))
      if (result !== false) resetForm()
    } finally {
      // Also runs when onSubmit throws, so the user can always try again.
      isSubmitting.value = false
    }
  }

  return { values, errors, isSubmitting, validate, setValues, resetForm, handleSubmit }
}
