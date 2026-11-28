import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StepProgress from '@/components/forms/StepProgress.vue'

const steps = [
  { id: 'personal', title: 'Persoonlijk' },
  { id: 'appointment', title: 'Afspraak' },
  { id: 'summary', title: 'Overzicht' },
]

describe('StepProgress', () => {
  it('shows the current progress', () => {
    const wrapper = mount(StepProgress, {
      props: { steps, current: 1 },
    })

    expect(wrapper.find('[data-testid="step-progress-label"]').text()).toBe('Stap 2 van 3')
  })

  it('sets the correct state for each step', () => {
    const wrapper = mount(StepProgress, {
      props: { steps, current: 1 },
    })

    expect(wrapper.find('[data-testid="step-indicator-personal"]').attributes('data-state')).toBe(
      'complete',
    )

    expect(
      wrapper.find('[data-testid="step-indicator-appointment"]').attributes('data-state'),
    ).toBe('current')

    expect(wrapper.find('[data-testid="step-indicator-summary"]').attributes('data-state')).toBe(
      'upcoming',
    )
  })

  it('shows the correct progress bar width', () => {
    const wrapper = mount(StepProgress, {
      props: { steps, current: 1 },
    })

    expect(wrapper.find('[data-testid="step-progress-bar"]').attributes('style')).toContain(
      '66.66666666666666%',
    )
  })

  it('makes completed steps clickable', () => {
    const wrapper = mount(StepProgress, {
      props: { steps, current: 2 },
    })

    expect(wrapper.find('[data-testid="step-indicator-personal"] button').exists()).toBe(true)
    expect(wrapper.find('[data-testid="step-indicator-appointment"] button').exists()).toBe(true)
    expect(wrapper.find('[data-testid="step-indicator-summary"] button').exists()).toBe(false)
  })

  it('emits select with the clicked step index', async () => {
    const wrapper = mount(StepProgress, {
      props: { steps, current: 2 },
    })

    await wrapper.find('[data-testid="step-indicator-personal"] button').trigger('click')

    expect(wrapper.emitted('select')).toEqual([[0]])
  })

  it('shows the current step with aria-current', () => {
    const wrapper = mount(StepProgress, {
      props: { steps, current: 1 },
    })

    expect(
      wrapper.find('[data-testid="step-indicator-appointment"]').attributes('aria-current'),
    ).toBe('step')

    expect(
      wrapper.find('[data-testid="step-indicator-personal"]').attributes('aria-current'),
    ).toBeUndefined()
  })
})
