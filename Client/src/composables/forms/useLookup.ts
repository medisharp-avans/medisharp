import { computed, ref, toValue, type MaybeRefOrGetter } from 'vue'

/**
 * Encapsulates the logic for a `DatalistInput` component.
 * 
 * Manages the typed text, filters and formats the available options, 
 * and tracks the exact matching item (e.g., to resolve and submit an ID instead of a name).
 * 
 * Items missing a valid key or label are automatically filtered out.
 * 
 * @template T - The type of the source items.
 * @param items - A reactive reference, getter, or array of raw data items.
 * @param getKey - Function to extract a unique key or ID from an item.
 * @param getLabel - Function to extract the display label string from an item.
 * @param getDescription - Optional function to extract a secondary description for an item.
 */
export function useLookup<T>(
  items: MaybeRefOrGetter<T[] | null | undefined>,
  getKey: (item: T) => string | number | null,
  getLabel: (item: T) => string | null,
  getDescription?: (item: T) => string | undefined,
) {
  const text = ref('')

  const options = computed(() =>
    (toValue(items) ?? []).flatMap((item) => {
      const key = getKey(item)
      const value = getLabel(item)
      return key !== null && value ? [{ key, value, description: getDescription?.(item) }] : []
    }),
  )

  const selected = computed(() =>
    (toValue(items) ?? []).find((item) => getKey(item) !== null && getLabel(item) === text.value),
  )

  return { text, options, selected }
}
