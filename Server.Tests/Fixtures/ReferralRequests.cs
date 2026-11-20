using Server.Contracts.Referrals;
using Server.Models;

namespace Server.Tests.Fixtures
{
    public class ReferralRequests
    {
        public CreateReferralRequest Valid() => new()
        {
            Bsn = "000000000", // Synthetic; this phase tests presence, not the BSN checksum.
            PatientName = "Testpatient",
            PatientAddress = "Teststraat 1",
            PatientCity = "Teststad",
            PatientPhoneNumber = "0000000000",
            PatientEmailAddress = "patient@example.test",
            Specialty = "KNO",
            TreatmentCode = "TEST-KNO",
            ReasonForReferral = "Aanhoudende oorpijn",
            ClinicalQuestion = "Wat is de oorzaak?",
            Medication = "Geen",
            MedicalInformation = "Alleen geselecteerde relevante gegevens",
            Allergies = "Geen bekend",
            Urgency = ReferralUrgency.Regular
        };
    }
}
