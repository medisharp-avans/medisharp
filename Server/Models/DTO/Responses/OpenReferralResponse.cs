namespace Server.Models.DTO.Responses;

/// <summary>Only the information needed to identify an open referral in the planning queue.</summary>
public record OpenReferralResponse(string Specialty, string TreatmentCode, string PatientName, string Bsn);
