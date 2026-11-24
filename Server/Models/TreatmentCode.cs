using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class TreatmentCode
    {
        [Required]
        [StringLength(20)]
        [Key]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(Specialism))]
        public Specialism Specialism { get; set; } 

        [Required]
        public int DurationInQuarters { get; set; }

        [Required]
        public decimal Cost { get; set; }
    }
}
