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

export enum ReferralUrgency
{
  Regulier = 0,
  Urgent = 1,
  Spoed = 2
}

export interface Patient {
  patientId: number | null
  patientFullName: string | null
}

export interface Specialist {
  specialistId: number | null
  specialistFullName: string | null
}

export interface TreatmentCode {
  code: string
  description: string
}
