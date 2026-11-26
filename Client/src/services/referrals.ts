export type ReferralDraft = Record<
  | 'specialty'
  | 'treatmentCode'
  | 'bsn'
  | 'patientName'
  | 'patientAddress'
  | 'patientCity'
  | 'patientPhoneNumber'
  | 'patientEmailAddress'
  | 'reasonForReferral'
  | 'medicalInformation'
  | 'medication'
  | 'allergies'
  | 'urgency',
  string
>

export type SaveResult = { saved: true } | { saved: false; message: string; fields: string[] }

/** Verstuurt patiëntgegevens uitsluitend in de POST-body. Geeft geen serverdetails door. */
export async function createReferral(
  draft: ReferralDraft,
  attachments: File[] = [],
): Promise<SaveResult> {
  try {
    const form = new FormData()
    form.append('referral', JSON.stringify(draft))
    attachments.forEach((file) => form.append('attachments', file))
    const response = await fetch(
      attachments.length ? '/api/referrals/with-attachments' : '/api/referrals',
      {
        method: 'POST',
        headers: attachments.length ? undefined : { 'Content-Type': 'application/json' },
        body: attachments.length ? form : JSON.stringify(draft),
      },
    )
    if (response.status === 201) return { saved: true }
    if (response.status === 413)
      return { saved: false, fields: ['attachments'], message: 'De bestanden zijn samen te groot.' }
    if (response.status === 400) {
      const data: unknown = await response.json()
      const errors = data && typeof data === 'object' && 'errors' in data ? data.errors : null
      const fields = errors && typeof errors === 'object' ? Object.keys(errors) : []
      return { saved: false, message: 'Controleer de ingevulde gegevens.', fields }
    }
    return {
      saved: false,
      fields: [],
      message:
        response.status === 404
          ? 'Verwijzingen aanmaken is in deze omgeving niet beschikbaar.'
          : 'Opslaan is niet gelukt. Je invoer blijft in het formulier staan.',
    }
  } catch {
    return {
      saved: false,
      fields: [],
      message:
        'Er kon geen bevestiging worden ontvangen. Controleer of de verwijzing is opgeslagen voordat je opnieuw verstuurt.',
    }
  }
}
