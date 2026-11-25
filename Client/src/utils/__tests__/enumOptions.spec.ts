import { describe, it, expect } from 'vitest'
import { enumOptions } from '@/utils/enumOptions'

describe('enumOptions', () => {
  it('converts a numeric enum into select options', () => {
    // Arrange
    const specialism = {
      0: 'Cardiologie',
      1: 'Dermatologie',
      Cardiologie: 0,
      Dermatologie: 1,
    }

    // Act
    const result = enumOptions(specialism)

    // Assert
    expect(result).toEqual([
      { value: 0, label: 'Cardiologie' },
      { value: 1, label: 'Dermatologie' },
    ])
  })

  it('filters out string values from the enum', () => {
    // Arrange
    const exampleEnum = {
      0: 'First',
      1: 'Second',
      First: 0,
      Second: 1,
      Description: 'Example',
    }

    // Act
    const result = enumOptions(exampleEnum)

    // Assert
    expect(result).toHaveLength(2)
    expect(result.every((option) => typeof option.value === 'number')).toBe(true)
  })

  it('returns an empty array when no numeric values exist', () => {
    // Arrange
    const exampleEnum = {
      First: 'One',
      Second: 'Two',
    }

    // Act
    const result = enumOptions(exampleEnum)

    // Assert
    expect(result).toEqual([])
  })

  it('returns an empty array when the enum is empty', () => {
    // Arrange
    const exampleEnum = {}

    // Act
    const result = enumOptions(exampleEnum)

    // Assert
    expect(result).toEqual([])
  })
})
