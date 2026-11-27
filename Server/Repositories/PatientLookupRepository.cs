using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.DTO.Responses;

namespace Server.Repositories;

public class PatientLookupRepository(AppDbContext context)
{
    public Task<ReferralPatientResponse?> FindByBsnAsync(string bsn, CancellationToken cancellationToken) =>
        context.Patients.AsNoTracking()
            .Where(patient => patient.Bsn == bsn)
            .Select(patient => new ReferralPatientResponse(patient.FullName, patient.PostalCode,
                patient.HouseNumber, patient.PhoneNumber, patient.Email))
            .SingleOrDefaultAsync(cancellationToken);
}
