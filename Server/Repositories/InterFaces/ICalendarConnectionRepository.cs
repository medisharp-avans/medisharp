using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Defines data access operations for managing a specialist's external iCal/ICS calendar connection.
    /// </summary>
    public interface ICalendarConnectionRepository : IRepository<UserCalendarConnection>
    {
        /// <summary>
        /// Retrieves the external calendar connection for the specified specialist, if one exists.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>The <see cref="UserCalendarConnection"/> if found; otherwise, <c>null</c>.</returns>
        Task<UserCalendarConnection?> GetConnectionUser(int specialistId);

        /// <summary>
        /// Creates a new calendar connection or updates the encrypted iCal/ICS URL of an existing connection.
        /// </summary>
        /// <param name="connection">The existing connection entity, or <c>null</c> if none exists yet.</param>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <param name="encryptedUrl">The encrypted iCal/ICS feed URL to persist.</param>
        /// <returns>The created or updated <see cref="UserCalendarConnection"/> entity.</returns>
        Task<UserCalendarConnection> UpdateUserCalendarConnection(
            UserCalendarConnection? connection,
            int specialistId,
            string encryptedUrl);

        /// <summary>
        /// Deletes the specialist's external calendar connection from the database.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous delete operation.</returns>
        Task DisconnectAsync(int specialistId);
    }
}
