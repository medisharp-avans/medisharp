
using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Defines data access operations for managing a specialist's external iCal/ICS calendar connection.
    /// </summary>
    public interface ICalendarConnectionRepository : IRepository<Specialist>
    {
        /// <summary>
        /// Retrieves the external calendar connection for the specified specialist, if one exists.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>The <see cref="Specialist"/> if found; otherwise, <c>null</c>.</returns>
        Task<Specialist?> GetConnectionUser(int specialistId);

        /// <summary>
        /// Updates the encrypted iCal/ICS URL of an existing specialist.
        /// </summary>
        /// <param name="specialist">The specialist whose calendar connection is being updated.</param>
        /// <param name="encryptedUrl">The encrypted iCal/ICS feed URL to persist.</param>
        /// <returns>The updated <see cref="Specialist"/> entity.</returns>
        Task<Specialist> UpdateUserCalendarConnection(
            Specialist specialist,
            string encryptedUrl);

        /// <summary>
        /// Deletes the specialist's external calendar connection from the database.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
        Task DisconnectAsync(int specialistId);
    }
}
