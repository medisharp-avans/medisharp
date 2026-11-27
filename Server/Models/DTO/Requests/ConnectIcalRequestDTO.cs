using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    public record ConnectIcalRequestDTO(
        [Required] int SpecialistId,
        [Required] string IcalUrl
    );
}
