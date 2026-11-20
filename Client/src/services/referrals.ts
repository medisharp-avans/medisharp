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
  | 'clinicalQuestion'
  | 'medicalInformation'
  | 'medication'
  | 'allergies'
  | 'urgency',
  string
>

export type SaveResult = { saved: true } | { saved: false; message: string; fields: string[] }

/** Verstuurt patiëntgegevens uitsluitend in de POST-body. Geeft geen serverdetails door. */
export async function createReferral(draft: ReferralDraft): Promise<SaveResult> {
  try {
    const response = await fetch('/api/referrals', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(draft),
    })
    if (response.status === 201) return { saved: true }
    if (response.status === 400) {
      const data = await response.json()
      const fields = data?.errors && typeof data.errors === 'object' ? Object.keys(data.errors) : []
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
