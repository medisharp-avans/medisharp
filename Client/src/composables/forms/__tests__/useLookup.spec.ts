import { describe, it, expect } from 'vitest'
import { ref } from 'vue'
import { useLookup } from '@/composables/forms/useLookup'

describe('useLookup', () => {
  type TestItem = {
    id: number | null
    name: string | null
    description?: string
  }

  const getKey = (item: TestItem) => item.id
  const getLabel = (item: TestItem) => item.name

  it('starts with empty text', () => {
    const { text } = useLookup<TestItem>([], getKey, getLabel)

    expect(text.value).toBe('')
  })

  it('creates options from the items', () => {
    const items: TestItem[] = [
      { id: 1, name: 'Jan' },
      { id: 2, name: 'Piet' },
    ]

    const { options } = useLookup(items, getKey, getLabel)

    expect(options.value).toEqual([
      { key: 1, value: 'Jan', description: undefined },
      { key: 2, value: 'Piet', description: undefined },
    ])
  })

  it('ignores items without a key or label', () => {
    const items: TestItem[] = [
      { id: 1, name: 'Jan' },
      { id: null, name: 'Piet' },
      { id: 3, name: null },
      { id: 4, name: '' },
    ]

    const { options } = useLookup(items, getKey, getLabel)

    expect(options.value).toEqual([{ key: 1, value: 'Jan', description: undefined }])
  })

  it('adds descriptions when provided', () => {
    const items: TestItem[] = [{ id: 1, name: 'Jan', description: 'Cardioloog' }]

    const { options } = useLookup(items, getKey, getLabel, (item) => item.description)

    expect(options.value).toEqual([{ key: 1, value: 'Jan', description: 'Cardioloog' }])
  })

  it('finds the selected item based on the text', () => {
    const items: TestItem[] = [
      { id: 1, name: 'Jan' },
      { id: 2, name: 'Piet' },
    ]

    const { text, selected } = useLookup(items, getKey, getLabel)

    text.value = 'Piet'

    expect(selected.value).toEqual({ id: 2, name: 'Piet' })
  })

  it('returns undefined when no item matches the text', () => {
    const items: TestItem[] = [{ id: 1, name: 'Jan' }]

    const { text, selected } = useLookup(items, getKey, getLabel)

    text.value = 'Piet'

    expect(selected.value).toBeUndefined()
  })

  it('updates options when the items change', () => {
    const items = ref<TestItem[]>([{ id: 1, name: 'Jan' }])

    const { options } = useLookup(items, getKey, getLabel)

    items.value.push({ id: 2, name: 'Piet' })

    expect(options.value).toEqual([
      { key: 1, value: 'Jan', description: undefined },
      { key: 2, value: 'Piet', description: undefined },
    ])
  })

  it('updates the selected item when the items change', () => {
    const items = ref<TestItem[]>([{ id: 1, name: 'Jan' }])

    const { text, selected } = useLookup(items, getKey, getLabel)

    text.value = 'Piet'

    expect(selected.value).toBeUndefined()

    items.value.push({ id: 2, name: 'Piet' })

    expect(selected.value).toEqual({ id: 2, name: 'Piet' })
  })
})
