
using Server.Models.Enums;

namespace Server.Models
{
    public class TreatmentCode
    {
        public string Code { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Specialism Specialism { get; set; }

        public int DurationInQuarters { get; set; }

        public decimal Cost { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
