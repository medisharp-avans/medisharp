namespace Server.Models
{
    /// <summary>A selectable treatment from the project's care treatment catalogue, not a patient appointment.</summary>
    public class Treatment
    {
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required string Specialty { get; set; }

        /// <summary>Duration as supplied in the catalogue: one quarter equals fifteen minutes.</summary>
        public required int DurationInQuarters { get; set; }

        /// <summary>Catalogue cost in euros; no invoice or charge is created by selecting a treatment.</summary>
        public required decimal CostEuro { get; set; }
    }
}
