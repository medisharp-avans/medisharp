import { computed, ref, toValue, type MaybeRefOrGetter } from 'vue'

/**
 * Logica achter een DatalistInput: de getypte tekst, de opties voor de lijst
 * en het item waar de tekst precies mee overeenkomt (bijvoorbeeld om het ID
 * te versturen in plaats van de naam).
 *
 * Items zonder sleutel of zonder label komen niet in de lijst.
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
