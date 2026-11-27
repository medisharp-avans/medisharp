using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Enums;
using Server.Tests.PageModels;
using Server.Tests.Utils;

namespace Server.Tests.EndToEnd;

[TestClass]
public class ReferralWorkflowTests : EndToEndTestBase
{
    /// <summary>The GP submits a complete referral and hospital staff find it in the open list.</summary>
    [TestMethod, TestCategory("TC-AFS-01a_1_1")]
    public async Task GpCreatesOpenReferralAndHospitalStaffCanSeeIt()
    {
        const string bsn = "000000001";
        const string name = "Fictieve AFS-01a-patiënt";
        const string specialty = "KNO";
        const string treatment = "Z0005";
        var login = new LoginPage(Driver);
        var navigation = new PortalNavigation(Driver);
        var referrals = new ReferralWorkflowPage(Driver);

        await using var intake = new ReferralDbContext(new DbContextOptionsBuilder<ReferralDbContext>()
            .UseSqlServer(TestSettings.ReferralConnectionString).Options);
        Assert.IsFalse(await intake.Patients.AnyAsync(patient => patient.Bsn == bsn),
            "The dedicated test BSN must be unused at the start of the scenario.");
        var appointmentCount = await DbContext.Appointments.CountAsync();

        try
        {
            login.LoginAs("visser@test.nl");
            navigation.GoToReferrals();
            referrals.CreateReferral(bsn, name, specialty, treatment);

            var saved = await intake.Referrals.AsNoTracking().SingleAsync(referral => referral.Bsn == bsn);
            Assert.AreEqual(ReferralStatus.Open, saved.Status);
            Assert.IsNotNull(saved.PatientId);
            Assert.IsNull(saved.PatientPhoneNumber);
            Assert.AreEqual(appointmentCount, await DbContext.Appointments.CountAsync());

            referrals.Logout();
            login.LoginAs("medewerker.a@test.nl");
            navigation.GoToReferrals();
            CollectionAssert.AreEqual(new[] { specialty, treatment, name, bsn }, referrals.FindOpenReferral(bsn));
        }
        finally
        {
            await intake.Referrals.Where(referral => referral.Bsn == bsn && referral.PatientName == name)
                .ExecuteDeleteAsync();
            await intake.Patients.Where(patient => patient.Bsn == bsn && patient.FullName == name)
                .ExecuteDeleteAsync();
        }
    }
}
