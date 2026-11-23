import { describe, it, expect, vi } from 'vitest'
import { useForm } from '@/composables/useForm'

const required = (value: string) => (value ? true : 'Required.')

describe('useForm', () => {
  it('does not submit while a rule fails', async () => {
    const onSubmit = vi.fn<() => void>()
    const { errors, handleSubmit } = useForm({
      initialValues: { name: '' },
      rules: { name: required },
      onSubmit,
    })

    await handleSubmit()

    expect(onSubmit).not.toHaveBeenCalled()
    expect(errors.value.name).toBe('Required.')
  })

  it('submits and resets the form when the rules pass', async () => {
    const onSubmit = vi.fn<() => void>()
    const { values, handleSubmit } = useForm({
      initialValues: { name: '' },
      rules: { name: required },
      onSubmit,
    })

    values.value.name = 'Ibuprofen'
    await handleSubmit()

    expect(onSubmit).toHaveBeenCalledWith({ name: 'Ibuprofen' })
    expect(values.value.name).toBe('')
  })

  it('keeps the input when onSubmit returns false', async () => {
    const { values, handleSubmit } = useForm({
      initialValues: { name: '' },
      onSubmit: () => false,
    })

    values.value.name = 'General practitioner'
    await handleSubmit()

    expect(values.value.name).toBe('General practitioner')
  })

  it('clears an old error once the value is valid', () => {
    const { values, errors, validate } = useForm({
      initialValues: { name: '' },
      rules: { name: required },
      onSubmit: vi.fn<() => void>(),
    })

    expect(validate()).toBe(false)
    values.value.name = 'Ibuprofen'

    expect(validate()).toBe(true)
    expect(errors.value).toEqual({})
  })

  it('does not submit twice while saving is in progress', async () => {
    let finishSaving = () => {}
    const onSubmit = vi.fn<() => Promise<void>>(
      () =>
        new Promise<void>((resolve) => {
          finishSaving = resolve
        }),
    )
    const { isSubmitting, handleSubmit } = useForm({ initialValues: { name: '' }, onSubmit })

    const firstSubmit = handleSubmit()
    await handleSubmit()
    expect(isSubmitting.value).toBe(true)

    finishSaving()
    await firstSubmit

    expect(onSubmit).toHaveBeenCalledTimes(1)
    expect(isSubmitting.value).toBe(false)
  })

  it('can submit again after onSubmit throws', async () => {
    const onSubmit = vi.fn<() => Promise<void>>().mockRejectedValueOnce(new Error('Server error'))
    const { values, isSubmitting, handleSubmit } = useForm({
      initialValues: { name: '' },
      onSubmit,
    })
    values.value.name = 'Ibuprofen'

    await expect(handleSubmit()).rejects.toThrow('Server error')

    expect(isSubmitting.value).toBe(false)
    expect(values.value.name).toBe('Ibuprofen')
  })

  it('fully restores nested initial values', () => {
    const { values, resetForm } = useForm({
      initialValues: { allergies: ['Peanut'] },
      onSubmit: vi.fn<() => void>(),
    })

    values.value.allergies.push('Latex')
    resetForm()

    expect(values.value.allergies).toEqual(['Peanut'])
  })

  it('fills the form with setValues and clears the errors', () => {
    const { values, errors, validate, setValues } = useForm({
      initialValues: { name: '' },
      rules: { name: required },
      onSubmit: vi.fn<() => void>(),
    })
    validate()

    setValues({ name: 'Paracetamol' })

    expect(values.value.name).toBe('Paracetamol')
    expect(errors.value).toEqual({})
  })
})
