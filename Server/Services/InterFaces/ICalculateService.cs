namespace Server.Services.InterFaces
{
    /// <summary>
    /// Provides calculation operations related to appointments and treatment timelines.
    /// </summary>
    public interface ICalculateService
    {
        /// <summary>
        /// Calculates the expected end time of an appointment based on its start time and the associated treatment code.
        /// </summary>
        /// <param name="startTime">The starting date and time of the appointment.</param>
        /// <param name="treatmentCode">The treatment code used to determine the duration of the appointment.</param>
        ///<returns>A <see cref="DateTime"/> representing the calculated end time.</returns>
        Task<DateTime> CalculateAppointmentEndTime(DateTime startTime, string treatmentCode);
    }
}