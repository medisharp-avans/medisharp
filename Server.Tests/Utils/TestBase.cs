using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Server.Data;
using Server.Tests.TestDb;
using Server.Tests.Utils;
using System.Diagnostics;

namespace Server.Tests
{
    [TestClass]
    public class TestBase
    {
        public TestContext TestContext { get; set; } = null!;
        private Stopwatch _stopwatch = null!;
        private const string FallbackConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=DeZachteHeelmeesters_DbTest;Trusted_Connection=True;TrustServerCertificate=True";

        private static string _connectionString = null!;

        protected static IConfiguration Config = null!;
        protected static string BaseUrl = null!;

        protected AppDbContext DbContext = null!;
        protected QueryPlans Db = null!;
        protected IWebDriver Driver = null!;

        [AssemblyInitialize]
        public static async Task AssemblyInit(TestContext _)
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json", optional: true)
                .Build();

            BaseUrl = (Config["BaseUrl"] ?? "https://localhost:5071").TrimEnd('/');
            _connectionString = Config["ConnectionStrings:DefaultConnection"] ?? FallbackConnectionString;

            // Veiligheidscheck: ClearData() wist alle tabellen, dus nooit op een niet-testdatabase.
            var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
            if (!dbName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Database '{dbName}' lijkt geen testdatabase. De tests wissen alle data, dus ik stop.");
            }

            using (var context = CreateDbContext())
            {
                context.Database.Migrate();
                TestDbInitializer.Seed(context);
            }

            await EnsureAppIsRunningAsync();
        }

        [TestInitialize]
        public void TestInit()
        {
            _stopwatch = Stopwatch.StartNew();
            TestLog.Logger.Information("▶ START  {Test}", TestContext.TestName);

            new TestMsSqlDatabaseHelper(_connectionString).ClearData();

            DbContext = CreateDbContext();
            TestDbInitializer.Seed(DbContext);
            Db = new QueryPlans(DbContext);

            var options = new ChromeOptions { AcceptInsecureCertificates = true };
            options.AddArgument("--ignore-certificate-errors");

            Driver = new ChromeDriver(options);
            Driver.Manage().Window.Maximize();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                var outcome = TestContext.CurrentTestOutcome;
                string? screenshot = null;

                if (outcome != UnitTestOutcome.Passed)
                    screenshot = TestLog.FailureScreenshot(Driver, TestContext.TestName!);

                TestLog.Logger.Information("■ EINDE  {Test} => {Outcome} ({Ms} ms) {Screenshot}",
                    TestContext.TestName, outcome, _stopwatch.ElapsedMilliseconds, screenshot);
            }
            finally
            {
                try { Driver?.Quit(); }
                finally
                {
                    Driver?.Dispose();
                    DbContext?.Dispose();
                }
            }
        }

        private static AppDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);

        /// <summary>Faalt snel en duidelijk als de app niet handmatig gestart is.</summary>
        private static async Task EnsureAppIsRunningAsync()
        {
            using var http = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            })
            { Timeout = TimeSpan.FromSeconds(5) };

            try
            {
                await http.GetAsync(BaseUrl);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new InvalidOperationException(
                    $"De applicatie draait niet op {BaseUrl}. Start de webapp eerst met het profiel 'Tests' " +
                    $"(zonder debuggen), zodat hij dezelfde LocalDB-database gebruikt als de tests.", ex);
            }
        }
    }

}