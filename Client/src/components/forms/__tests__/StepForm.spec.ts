import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import StepForm from '@/components/forms/StepForm.vue'

const steps = [
  { id: 'personal', title: 'Persoonlijke gegevens', description: 'Vul je gegevens in.' },
  { id: 'appointment', title: 'Afspraak', description: 'Kies een afspraak.' },
  { id: 'summary', title: 'Overzicht' },
]

describe('StepForm', () => {
  it('shows the current step and its slot', () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 0 },
      slots: {
        'step-personal': '<input data-testid="personal-input" />',
      },
    })

    expect(wrapper.find('[data-testid="step-personal"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="personal-input"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="step-appointment"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="step-title"]').text()).toBe('Persoonlijke gegevens')
  })

  it('emits update:modelValue when clicking next', async () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 0 },
      attachTo: document.body,
    })

    await wrapper.find('[data-testid="step-next"]').trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[1]])
  })

  it('emits update:modelValue when clicking previous', async () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 1 },
    })

    await wrapper.find('[data-testid="step-previous"]').trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[0]])
  })

  it('emits submit on the last step', async () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 2 },
    })

    await wrapper.find('[data-testid="step-form"]').trigger('submit')

    expect(wrapper.emitted('submit')).toHaveLength(1)
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
  })

  it('does not emit submit when it is not the last step', async () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 0 },
    })

    await wrapper.find('[data-testid="step-form"]').trigger('submit')

    expect(wrapper.emitted('submit')).toBeUndefined()
    expect(wrapper.emitted('update:modelValue')).toEqual([[1]])
  })

  it('disables the submit button while loading', () => {
    const wrapper = mount(StepForm, {
      props: {
        steps,
        modelValue: 2,
        loading: true,
      },
    })

    const button = wrapper.find('[data-testid="submit"]')

    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toBe('Bezig...')
  })

  it('uses the custom submit label', () => {
    const wrapper = mount(StepForm, {
      props: {
        steps,
        modelValue: 2,
        submitLabel: 'Afspraak aanvragen',
      },
    })

    expect(wrapper.find('[data-testid="submit"]').text()).toBe('Afspraak aanvragen')
  })

  it('moves focus to the heading when the step changes', async () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 0 },
      attachTo: document.body,
    })

    await wrapper.setProps({ modelValue: 1 })
    await nextTick()

    expect(document.activeElement).toBe(wrapper.find('[data-testid="step-title"]').element)
  })

  it('allows returning to a completed step through StepProgress', async () => {
    const wrapper = mount(StepForm, {
      props: { steps, modelValue: 2 },
    })

    const firstStep = wrapper.find('[data-testid="step-indicator-personal"]')

    await firstStep.find('button').trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[0]])
  })
})
