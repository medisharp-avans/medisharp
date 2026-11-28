namespace Server.Models.Intake;

/// <summary>Patient identity and contact data. A patient can exist before portal registration.</summary>
public class Patient
{
    public int Id { get; set; }
    public required string Bsn { get; init; }
    public required string FullName { get; set; }
    public required string PostalCode { get; set; }
    public required string HouseNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Email { get; set; }
}
