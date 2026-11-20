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
