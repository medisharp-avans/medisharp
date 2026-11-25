namespace Server.Services.InterFaces
{
    public interface IEmailService
    {
        /// <summary>Sends a plain text email to one recipient.</summary>
        Task SendAsync(string to, string subject, string body);
    }
}
