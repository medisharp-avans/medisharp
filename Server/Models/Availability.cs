using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Availability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpecialistId { get; set; }

        public Specialist Specialist { get; set; } = null!;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }
    }
}