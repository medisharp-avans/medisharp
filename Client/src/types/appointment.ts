/**
 * Spiegelt Server.Models.Enums.Specialism. De API stuurt en verwacht een getal,
 * tenzij JsonStringEnumConverter is ingesteld. De volgorde moet dus gelijk blijven aan de C#-enum.
 */
export enum Specialism {
  Cardiologie = 0,
  Dermatologie = 1,
  Longgeneeskunde = 2,
  Oogheelkunde = 3,
  KNO = 4,
  Orthopedie = 5,
  Chirurgie = 6,
  Radiologie = 7,
  Laboratoriumgeneeskunde = 8,
  Revalidatiegeneeskunde = 9,
  Psychiatrie = 10,
  Algemeen = 11,
}

/** Spiegelt AppointmentManualRequestDTO (zonder verwijzing: die wordt door de API aangemaakt). */
export interface AppointmentManualRequest {
  patientId: number
  specialistId: number
  /** ISO-datum zonder tijdzone, bijvoorbeeld 2027-01-12T10:00:00 */
  startTime: string
  generalPractitionerId: number
  specialism: Specialism
  preferredSpecialistId: number | null
  reason: string
  questionnaire: string | null
  urgency: string
  treatmentCode: string
}

/** Antwoord van POST /api/appointments (Appointment-entity, zonder navigaties). */
export interface AppointmentResponse {
  id: number
  patientId: number
  specialistId: number
  referralId: number | null
  startTime: string
  endTime: string
  status: number
}

/** GET /api/patients/{id} */
export interface PatientDetail {
  id: number
  name: string
  /** Patient.GeneralPractitionerId is optioneel in het model */
  generalPractitionerId: number | null
}

/** GET /api/referrals/{id} */
export interface ReferralDetail {
  id: number
  patientId: number
  patientName: string
  generalPractitionerId: number
  specialism: Specialism
  preferredSpecialistId: number | null
  reason: string
  questionnaire: string | null
  urgency: string
  treatmentCode: string
  status: string
}

/** GET /api/treatmentcodes?specialism=KNO */
export interface TreatmentCodeOption {
  code: string
  description: string
  durationInQuarters: number
}

/** GET /api/timeslots?specialism=KNO&treatmentCode=KNO002&from=2027-01-11&days=14 */
export interface TimeSlot {
  specialistId: number
  specialistName: string
  /** ISO zonder tijdzone, bijvoorbeeld 2027-01-12T10:00:00 */
  startTime: string
}

/** GET /api/referrals */
export interface ReferralListItem {
  id: number
  patientName: string
  specialism: Specialism
  urgency: string
  status: string
}