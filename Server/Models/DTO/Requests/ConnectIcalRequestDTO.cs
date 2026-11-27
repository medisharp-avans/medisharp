using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    public record ConnectIcalRequestDTO(
        [Range(1, int.MaxValue)] int SpecialistId,
        [Required]
        [StringLength(2048)]
        string IcalUrl
    );
}