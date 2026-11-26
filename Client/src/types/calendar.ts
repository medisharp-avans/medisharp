export interface ConnectIcalRequest {
  specialistId: number
  icalUrl: string
}

export interface ConnectIcalResponse {
  message: string
}

export interface CalendarStatus {
  connected: boolean
}
