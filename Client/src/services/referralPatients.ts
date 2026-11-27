import { referralRequest } from './referralRequest'

export interface ReferralPatient {
  patientName: string
  patientPostalCode: string
  patientHouseNumber: string
  patientPhoneNumber: string | null
  patientEmailAddress: string
}

/** The lookup uses a request body so the BSN is never part of the URL. */
export async function findReferralPatient(
  bsn: string,
  signal: AbortSignal,
): Promise<ReferralPatient | null> {
  const failureMessage = 'De patiëntgegevens konden niet worden opgehaald.'
  const response = await referralRequest('/api/referrals/patient-lookup', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ bsn }),
    signal,
  }).catch(() => {
    throw new Error(failureMessage)
  })
  if (response.status === 204) return null
  if (response.status === 401 || response.status === 403) {
    throw new Error('Zoeken op BSN is alleen beschikbaar voor ingelogde huisartsen.')
  }
  if (!response.ok) throw new Error(failureMessage)

  const data: unknown = await response.json().catch(() => null)
  if (
    !data ||
    typeof data !== 'object' ||
    !('patientName' in data && typeof data.patientName === 'string') ||
    !('patientPostalCode' in data && typeof data.patientPostalCode === 'string') ||
    !('patientHouseNumber' in data && typeof data.patientHouseNumber === 'string') ||
    !('patientEmailAddress' in data && typeof data.patientEmailAddress === 'string') ||
    !(
      'patientPhoneNumber' in data &&
      (data.patientPhoneNumber === null || typeof data.patientPhoneNumber === 'string')
    )
  ) {
    throw new Error(failureMessage)
  }
  return {
    patientName: data.patientName,
    patientPostalCode: data.patientPostalCode,
    patientHouseNumber: data.patientHouseNumber,
    patientPhoneNumber: data.patientPhoneNumber,
    patientEmailAddress: data.patientEmailAddress,
  }
}
