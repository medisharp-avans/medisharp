import { describe, it, expect } from 'vitest'
import { useFormMessage } from '@/composables/forms/useFormMessage'
import axios from 'axios'

describe('useFormMessage', () => {
  it('starts without a message', () => {
    const { message } = useFormMessage()

    expect(message.value).toBeNull()
  })

  it('sets a success message', () => {
    const { message, success } = useFormMessage()

    success('Opgeslagen!')

    expect(message.value).toEqual({
      type: 'success',
      text: 'Opgeslagen!',
    })
  })

  it('sets an error message', () => {
    const { message, error } = useFormMessage()

    error('Er is iets fout gegaan.')

    expect(message.value).toEqual({
      type: 'error',
      text: 'Er is iets fout gegaan.',
    })
  })

  it('clears the message', () => {
    const { message, success, clear } = useFormMessage()

    success('Opgeslagen!')
    clear()

    expect(message.value).toBeNull()
  })

  it('uses the server message when an Axios error contains a string response', () => {
    const { message, fail } = useFormMessage()

    const serverError = new axios.AxiosError('Request failed')
    serverError.response = {
      data: 'Deze afspraak bestaat al.',
      status: 409,
      statusText: 'Conflict',
      headers: {},
      config: { headers: new axios.AxiosHeaders() },
    }

    fail(serverError, 'Er is iets fout gegaan.')

    expect(message.value).toEqual({
      type: 'error',
      text: 'Deze afspraak bestaat al.',
    })
  })

  it('uses the fallback when the error is not an Axios error', () => {
    const { message, fail } = useFormMessage()

    fail(new Error('Something went wrong'), 'Er is iets fout gegaan.')

    expect(message.value).toEqual({
      type: 'error',
      text: 'Er is iets fout gegaan.',
    })
  })
})
