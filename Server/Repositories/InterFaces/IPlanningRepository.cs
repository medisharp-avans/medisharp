using Server.Models;
using Server.Models.DTO.Responses;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Defines data access operations for appointment scheduling, specialist lookups, and availability checks.
    /// </summary>
    public interface IPlanningRepository : IRepository<Patient>
    {
        /// <summary>
        /// Retrieves a list of all patients formatted as planning response data transfer objects.
        /// </summary>
        /// <returns>A list of <see cref="PlanningResponseDTO"/> representing all patients.</returns>
        Task<List<PlanningResponseDTO>> GetAllPatientsAsync();

        /// <summary>
        /// Retrieves treatment codes associated with a specific medical specialism.
        /// </summary>
        /// <param name="spec">The name or code of the specialism.</param>
        /// <returns>A list of matching <see cref="TreatmentCode"/> entities.</returns>
        Task<List<TreatmentCode>> GetCodesBySpecialism(string spec);

        /// <summary>
        /// Retrieves specialists associated with a specific medical specialism, formatted as planning response data transfer objects.
        /// </summary>
        /// <param name="spec">The name or code of the specialism.</param>
        /// <returns>A list of <see cref="PlanningResponseDTO"/> representing the specialists.</returns>
        Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec);

        /// <summary>
        /// Checks whether the specified specialist has any non-cancelled appointments that overlap with the given time range <paramref name="start"/> (inclusive) to <paramref name="end"/> (exclusive).
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <param name="start">The start time of the range to check.</param>
        /// <param name="end">The end time of the range to check.</param>
        /// <returns><c>true</c> if an overlapping appointment exists; otherwise, <c>false</c>.</returns>
        Task<bool> HasOverlapAsync(int specialistId, DateTime start, DateTime end);

        /// <summary>
        /// Retrieves all combined busy time intervals for a specialist within the specified local search window,
        /// merging active internal appointments and synced external calendar events (converted to Dutch local time).
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <param name="windowStart">The start of the search window in Dutch local time.</param>
        /// <param name="windowEnd">The end of the search window in Dutch local time.</param>
        /// <returns>A list of <c>(Start, End)</c> tuples in Dutch local time representing unavailable intervals.</returns>
        Task<List<(DateTime Start, DateTime End)>> GetBusyIntervalsAsync(int specialistId, DateTime windowStart, DateTime windowEnd);
    }
}
