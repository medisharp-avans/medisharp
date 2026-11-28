using Server.Models;
using System.Text.Json;

namespace Server.Data.SeedData
{
    /// <summary>Reads the versioned catalogue embedded in the application for EF Core model-managed data.</summary>
    internal class TreatmentSeedData
    {
        /// <summary>Loads deterministic reference data so migrations track additions, corrections and removals.</summary>
        public Treatment[] Read()
        {
            using var stream = typeof(TreatmentSeedData).Assembly
                .GetManifestResourceStream("Server.Data.SeedData.treatments.json")
                ?? throw new InvalidOperationException("The embedded treatment catalogue is missing.");

            return JsonSerializer.Deserialize<Treatment[]>(stream)
                ?? throw new InvalidOperationException("The embedded treatment catalogue is empty.");
        }
    }
}
