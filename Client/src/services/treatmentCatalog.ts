/** Haalt de beschikbare specialismen uit de behandelcatalogus op. */
export async function getSpecialties(signal?: AbortSignal): Promise<string[]> {
  const response = await fetch('/api/treatments/specialties', { signal })
  if (!response.ok) throw new Error('Catalogus niet beschikbaar')
  const data: unknown = await response.json()
  if (!Array.isArray(data) || !data.every((item) => typeof item === 'string')) {
    throw new Error('Ongeldige catalogusrespons')
  }
  return data
}
export interface TreatmentChoice {
  code: string
  description: string
  specialty: string
}

/** Haalt behandelingen op voor precies het gekozen specialisme. */
export async function getTreatments(
  specialty: string,
  signal?: AbortSignal,
): Promise<TreatmentChoice[]> {
  const query = new URLSearchParams({ specialty })
  const response = await fetch(`/api/treatments?${query}`, { signal })
  if (!response.ok) throw new Error('Catalogus niet beschikbaar')
  const data: unknown = await response.json()
  if (
    !Array.isArray(data) ||
    !data.every(
      (item) =>
        item !== null &&
        typeof item === 'object' &&
        typeof item.code === 'string' &&
        typeof item.description === 'string' &&
        item.specialty === specialty,
    )
  ) {
    throw new Error('Ongeldige catalogusrespons')
  }
  return data
}
