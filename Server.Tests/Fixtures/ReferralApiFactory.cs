using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Models.Enums;
using Server.Models.Intake;

namespace Server.Tests.Fixtures
{
    // Every test owns a separate relational database, closed and discarded with its factory.
    // SQL Server migration SQL is verified separately; it cannot be applied to SQLite.
    public class ReferralApiFactory(string environmentName = "Development") : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly SqliteConnection _accounts = new("Data Source=:memory:");
        private const string JwtIssuer = "ReferralApiTests";
        private const string JwtAudience = "ReferralApiTestClient";
        private const string TestTokenKey = "referral-api-tests-signing-key-for-isolated-test-hosts-only";
        private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes(TestTokenKey));

        public static HttpClient CreateClientAs(WebApplicationFactory<Program> factory, UserRole role = UserRole.Huisarts)
        {
            var client = factory.CreateClient();
            var jwt = new JwtSecurityToken(JwtIssuer, JwtAudience,
                [new Claim(ClaimTypes.Role, role.ToString())],
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                new JwtSecurityTokenHandler().WriteToken(jwt));
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting("ConnectionStrings:DefaultConnection", "UnusedTestConnection");
            builder.UseSetting("ConnectionStrings:ReferralConnection", "UnusedReferralTestConnection");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
            builder.UseSetting("JWT:Issuer", JwtIssuer);
            builder.UseSetting("JWT:Audience", JwtAudience);
            builder.UseSetting("JWT:Token", TestTokenKey);
            // Tests write to captured console output, not the machine's Windows Event Log.
            builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole()
                .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.Configure<KeyManagementOptions>(options =>
                    options.XmlRepository = new InMemoryKeyRepository());
                services.RemoveAll<ReferralDbContext>();
                services.RemoveAll<DbContextOptions<ReferralDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ReferralDbContext>>();
                _connection.Open();
                services.AddScoped<ReferralDbContext>(_ => new SqliteReferralDbContext(
                    new DbContextOptionsBuilder<ReferralDbContext>().UseSqlite(_connection).Options));
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                _accounts.Open();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_accounts));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReferralDbContext>();
            if (!context.Database.IsSqlite())
            {
                throw new InvalidOperationException("API tests must use their own SQLite database.");
            }

            context.Database.EnsureCreated();
            var accounts = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!accounts.Database.IsSqlite())
                throw new InvalidOperationException("API tests must use an isolated SQLite accounts database.");
            accounts.Database.EnsureCreated();
            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
                _accounts.Dispose();
            }
        }

        private class SqliteReferralDbContext(DbContextOptions<ReferralDbContext> options) : ReferralDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // SQLite sorts integers instead of DateTimeOffset values. All referral times use UTC.
                modelBuilder.Entity<Referral>().Property(referral => referral.CreatedAtUtc)
                    .HasConversion(timestamp => timestamp.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
            }
        }

        private class InMemoryKeyRepository : IXmlRepository
        {
            private readonly ConcurrentQueue<XElement> _elements = new();

            public IReadOnlyCollection<XElement> GetAllElements() => _elements.ToArray();

            public void StoreElement(XElement element, string friendlyName) => _elements.Enqueue(element);
        }
    }
}
