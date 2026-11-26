using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using Server.Models;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;


namespace Server.Services
{
    public class CalendarSyncService : IAgendaService
    {
        private readonly ICalendarConnectionRepository _connectionRepository;
        private readonly ICalendarEventRepository _eventRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IcalUrlProtector _protector;

        public CalendarSyncService(
            IHttpClientFactory httpClientFactory,
            IcalUrlProtector protector,
            ICalendarConnectionRepository connectionRepository,
            ICalendarEventRepository eventRepository)
        {
            _connectionRepository = connectionRepository;
            _eventRepository = eventRepository;
            _httpClientFactory = httpClientFactory;
            _protector = protector;
        }

        public async Task SaveAndSyncUrlAsync(int specialistId, string rawUrl)
        {
            var cleanUrl = rawUrl.Trim();
            if (cleanUrl.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase))
                cleanUrl = "https://" + cleanUrl["webcal://".Length..];

            string icsContent;
            try
            {
                if (!Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri) ||
                    uri.Scheme is not ("http" or "https"))
                    throw new ArgumentException("Ongeldige iCal-link.");

                icsContent = await _httpClientFactory.CreateClient().GetStringAsync(uri);

                if (!icsContent.Contains("BEGIN:VCALENDAR"))
                    throw new ArgumentException("Ongeldige iCal-link.");

                _ = Calendar.Load(icsContent); 
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                throw new ArgumentException("Ongeldige iCal-link.", ex);
            }

            var encryptedUrl = _protector.Protect(cleanUrl);
            var connection = await _connectionRepository.GetConnectionUser(specialistId);
            connection = await _connectionRepository.UpdateUserCalendarConnection(connection, specialistId, encryptedUrl);

            await SyncUserEventsAsync(connection, icsContent);
        }

        public async Task SyncUserEventsAsync(UserCalendarConnection connection, string? preloadedIcs = null)
        {
            var url = _protector.Unprotect(connection.EncryptedIcalUrl);
            var icsContent = preloadedIcs ?? await _httpClientFactory.CreateClient().GetStringAsync(url);

            var calendar = Calendar.Load(icsContent);
            var fromUtc = DateTime.UtcNow.AddMonths(-1);
            var toUtc = DateTime.UtcNow.AddMonths(6);

            var startCal = new CalDateTime(fromUtc, "UTC");
            var endCal = new CalDateTime(toUtc, "UTC");

            var options = new EvaluationOptions
            {
                MaxUnmatchedIncrementsLimit = 1000 
            };

            var occurrences = calendar.GetOccurrences(startCal, options)
                .TakeWhile(occ => occ.Period.StartTime <= endCal)
                .Take(2000) // Extra veiligheidslimiet op totaal aantal events
                .ToList();
            var fetchedEvents = new Dictionary<string, ExternalCalendarEvent>();

            foreach (var occ in occurrences)
            {
                if (occ.Source is not CalendarEvent ev) continue;

                var uid = $"{ev.Uid}_{occ.Period.StartTime.AsUtc:yyyyMMddTHHmmssZ}";
                fetchedEvents[uid] = new ExternalCalendarEvent
                {
                    SpecialistId = connection.SpecialistId,
                    ExternalUid = uid,
                    Title = ev.Summary ?? "Bezet",
                    StartUtc = occ.Period.StartTime.AsUtc,
                    EndUtc = occ.Period.EndTime?.AsUtc ?? occ.Period.StartTime.AsUtc,
                    IsAllDay = ev.IsAllDay
                };
            }

            // Database-afhandeling uitbesteed aan de repository
            await _eventRepository.SyncEventsAsync(
                connection.SpecialistId,
                fromUtc,
                toUtc,
                fetchedEvents,
                connection);
        }

        public async Task<bool> IsConnectedAsync(int specialistId)
        {
            return await _connectionRepository.GetConnectionUser(specialistId) != null;
        }

        public Task DisconnectAsync(int specialistId)
        {
            return _connectionRepository.DisconnectAsync(specialistId);
        }
    }
}
