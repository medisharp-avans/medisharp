/** Mirrors Server.Models.Enums.ClaimStatus (serialized as a number). */
export enum ClaimStatus {
  ToReview = 0,
  ReadyToSend = 1,
  Processed = 2,
  HandledManually = 3,
}

/** Dutch labels shown to the user. */
export const claimStatusLabels: Record<ClaimStatus, string> = {
  [ClaimStatus.ToReview]: 'Te controleren',
  [ClaimStatus.ReadyToSend]: 'Klaar om te versturen',
  [ClaimStatus.Processed]: 'Verwerkt',
  [ClaimStatus.HandledManually]: 'Handmatig afgehandeld',
}

/** Tailwind classes for the status badge, so every view shows a status in the same colour. */
export const claimStatusClasses: Record<ClaimStatus, string> = {
  [ClaimStatus.ToReview]: 'bg-amber-100 text-amber-900 border-amber-300',
  [ClaimStatus.ReadyToSend]: 'bg-blue-100 text-blue-900 border-blue-200',
  [ClaimStatus.Processed]: 'bg-green-100 text-green-900 border-green-200',
  [ClaimStatus.HandledManually]: 'bg-gray-100 text-gray-800 border-gray-200',
}

/** Columns the server can sort GET api/Claims on (PagedQuery.SortBy, case-insensitive text). */
export type ClaimSortField =
  | 'Id'
  | 'Patient'
  | 'PolicyNumber'
  | 'Status'
  | 'LineCount'
  | 'TotalAmount'
  | 'ReviewedBy'
  | 'CreatedAt'
  | 'LastModifiedAt'

/** One row of GET api/Claims (Server.Models.DTO.Responses.ClaimOverviewDTO). */
export interface ClaimOverview {
  id: number
  patientId: number
  patientName: string
  policyNumber: string
  status: ClaimStatus
  lineCount: number
  totalAmount: number
  /** Null while the claim is not reviewed yet. */
  reviewedByName: string | null
  createdAt: string
  submittedAt: string | null
  lastModifiedAt: string
}
