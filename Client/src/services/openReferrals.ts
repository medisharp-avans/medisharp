import { referralRequest } from './referralRequest'

export interface OpenReferral {
  specialty: string
  treatmentCode: string
  patientName: string
  bsn: string
}

/** Haalt één pagina met open verwijzingen op voor de ingelogde ziekenhuismedewerker. */
export async function getOpenReferrals(page: number): Promise<OpenReferral[]> {
  const failureMessage = 'De open verwijzingen konden niet worden opgehaald.'
  const response = await referralRequest(`/api/referrals/open?page=${page}`).catch(() => {
    throw new Error(failureMessage)
  })
  if (response.status === 401 || response.status === 403) {
    throw new Error('Dit overzicht is alleen beschikbaar voor bevoegd ziekenhuispersoneel.')
  }
  if (!response.ok) throw new Error(failureMessage)
  const data: unknown = await response.json().catch(() => null)
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
    throw new Error(failureMessage)
  }
  return data
}
