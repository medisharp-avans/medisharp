namespace Server.Contracts.Treatments
{
    /// <summary>Reference information for displaying a treatment choice in the referral form.</summary>
    public class TreatmentResponse
    {
        public required string Code { get; init; }
        public required string Description { get; init; }
        public required string Specialty { get; init; }
        public required int DurationInQuarters { get; init; }
        public required decimal CostEuro { get; init; }
    }
}
