namespace Server.Models.DTO.Responses;

/// <summary>Only patient fields needed for the referral form.</summary>
public record ReferralPatientResponse(
    string PatientName, string PatientPostalCode, string PatientHouseNumber,
    string? PatientPhoneNumber, string PatientEmailAddress);
