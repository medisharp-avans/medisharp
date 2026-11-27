

namespace Server.Services.InterFaces
{
    public interface ICalculateService 
    {
        Task<DateTime> CalculateAppointmentEndTime(DateTime startTime, string treatmentCode);
    }
}
