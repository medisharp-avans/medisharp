import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseForm from '@/components/forms/BaseForm.vue'
import BaseInput from '@/components/forms/BaseInput.vue'
import BaseSelect from '@/components/forms/BaseSelect.vue'
import BaseTextarea from '@/components/forms/BaseTextarea.vue'
import SubmitButton from '@/components/forms/SubmitButton.vue'

describe('form components', () => {
  it('BaseForm emits submit', async () => {
    const wrapper = mount(BaseForm)

    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')).toHaveLength(1)
  })

  it('BaseInput links the label and error message to the field', () => {
    const wrapper = mount(BaseInput<string>, {
      props: { id: 'name', label: 'Name', modelValue: '', error: 'Required.' },
    })
    const input = wrapper.find('input')

    expect(wrapper.find('label').attributes('for')).toBe('name')
    expect(input.attributes('aria-invalid')).toBe('true')
    expect(input.attributes('aria-describedby')).toBe('name-error')
    expect(wrapper.find('#name-error').text()).toBe('Required.')
    expect(wrapper.find('#name-error').attributes('role')).toBe('alert')
  })

  it('BaseInput updates the model and passes on attributes', async () => {
    const wrapper = mount(BaseInput<string>, {
      props: { label: 'E-mail', modelValue: '' },
      attrs: { type: 'email' },
    })
    const input = wrapper.find('input')

    await input.setValue('test@example.com')

    expect(input.attributes('type')).toBe('email')
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual(['test@example.com'])
  })

  it('BaseTextarea shows the counter and links it to the field', () => {
    const wrapper = mount(BaseTextarea, {
      props: { id: 'note', label: 'Note', modelValue: 'Hello', maxlength: 200 },
    })

    expect(wrapper.find('#note-hint').text()).toBe('5 / 200 tekens')
    expect(wrapper.find('textarea').attributes('aria-describedby')).toBe('note-hint')
  })

  it('BaseSelect keeps numbers as values', async () => {
    const wrapper = mount(BaseSelect<number>, {
      props: {
        label: 'Rule',
        modelValue: '',
        placeholder: 'Choose a rule',
        options: [
          { value: 1, label: 'Consult' },
          { value: 2, label: 'Home visit' },
        ],
      },
    })

    await wrapper.find('select').setValue(2)

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([2])
  })

  it('SubmitButton is disabled while loading', () => {
    const wrapper = mount(SubmitButton, {
      props: { loading: true, loadingLabel: 'Busy...' },
      slots: { default: 'Save' },
    })
    const button = wrapper.find('button')

    expect(button.attributes('disabled')).toBeDefined()
    expect(button.attributes('aria-busy')).toBe('true')
    expect(button.text()).toBe('Busy...')
  })
})
