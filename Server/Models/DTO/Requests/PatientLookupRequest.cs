using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests;

public record PatientLookupRequest(
    [Required, RegularExpression("^[0-9]{9}$")] string Bsn);
