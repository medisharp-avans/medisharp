export interface OpenReferral {
  specialty: string
  treatmentCode: string
  patientName: string
  bsn: string
}

export async function getOpenReferrals(page: number): Promise<OpenReferral[]> {
  const response = await fetch(`/api/referrals/open?page=${page}`, { cache: 'no-store' })
  if (response.status === 401 || response.status === 403) {
    throw new Error('Dit overzicht is alleen beschikbaar voor bevoegd ziekenhuispersoneel.')
  }
  if (!response.ok) throw new Error('De open verwijzingen konden niet worden opgehaald.')
  const data: unknown = await response.json()
  if (
    !Array.isArray(data) ||
    !data.every(
      (row) =>
        row &&
        typeof row.specialty === 'string' &&
        typeof row.treatmentCode === 'string' &&
        typeof row.patientName === 'string' &&
        typeof row.bsn === 'string',
    )
  ) {
    throw new Error('De open verwijzingen konden niet worden opgehaald.')
  }
  return data
}
