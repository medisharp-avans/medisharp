using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Server.Configuration;
using Server.Data;
using Server.Repositories;
using Server.Services;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Server
{
    /// <summary>Configures the API, its scoped dependencies and database startup.</summary>
    public class Program
    {
        /// <summary>Applies pending migrations when enabled and starts the HTTP server.</summary>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Locally via dotnet user-secrets, in Docker via ConnectionStrings__DefaultConnection (see README).
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is missing. Set it locally with: " +
                    "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=localhost,1433;...\"");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
            var applyMigrations = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
            var referralConnection = builder.Configuration.GetConnectionString("ReferralConnection")
                ?? BuildReferralConnection(connectionString);
            if (applyMigrations) ValidateSeparateDatabases(connectionString, referralConnection);
            builder.Services.AddDbContext<ReferralDbContext>(options => options.UseSqlServer(referralConnection));
            builder.Services.AddRegisteredRepositories();
            builder.Services.AddRegisteredServices();

            if (builder.Environment.IsDevelopment() && applyMigrations &&
                builder.Configuration.GetValue("Database:SeedDemoData", true))
            {
                builder.Services.AddHostedService<DevelopmentDataSeeder>();
            }

            // JWT login. The key comes from JWT:Token (appsettings.Development.json locally,
            // JWT__Token environment variable in production). Startup fails when it is missing or too short.
            builder.Services.AddOptions<JwtOptions>()
                .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.Configure<SMTPOptions>(builder.Configuration.GetSection(SMTPOptions.SectionName));

            var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Token)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };
                });
            builder.Services.AddAuthorization();

            // HTTPS is handled by the reverse proxy; take the scheme and client IP from the X-Forwarded-* headers.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // The proxy runs in a Docker network with a changing IP, so do not restrict this to localhost.
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Client", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddControllers(options =>
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
                .AddJsonOptions(options =>
                {
                    options.AllowInputFormatterExceptionMessages = false;
                    options.JsonSerializerOptions.ReferenceHandler =
                        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Place your JWT here (without 'Bearer ' in it!)."
                });

                // Pas het schema toe op alle endpoints
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            var app = builder.Build();

            // Apply pending EF Core migrations on startup (replaces Database/index.sql and db-init).
            // Integration tests provide their own isolated database and initialize its schema.
            if (applyMigrations)
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                await scope.ServiceProvider.GetRequiredService<ReferralDbContext>().Database.MigrateAsync();
            }

            app.UseForwardedHeaders();

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                // Exception text can contain patient values. Log only a safe event below.
                SuppressDiagnosticsCallback = _ => true,
                ExceptionHandler = HandleUnexpectedErrorAsync
            });

            // Configure the HTTP request pipeline.
            // Swagger in Development and Staging (acceptance), not in Production.
            if (!app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("Client");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            await app.RunAsync();
        }

        private static string BuildReferralConnection(string connectionString)
        {
            var referral = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(referral.InitialCatalog))
                throw new InvalidOperationException("Set a database name in ConnectionStrings:DefaultConnection or configure ReferralConnection.");

            referral.InitialCatalog += "_AFS01a";
            return referral.ConnectionString;
        }

        private static void ValidateSeparateDatabases(string connectionString, string referralConnection)
        {
            var planning = new SqlConnectionStringBuilder(connectionString);
            var referral = new SqlConnectionStringBuilder(referralConnection);
            if (string.Equals(planning.DataSource, referral.DataSource, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(planning.InitialCatalog, referral.InitialCatalog, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Planning and referral intake require separate databases.");
        }

        /// <summary>Returns a consistent error without copying exception or request details into the response or log.</summary>
        private static async Task HandleUnexpectedErrorAsync(HttpContext context)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("An unexpected API error occurred. TraceId: {TraceId}", context.TraceIdentifier);

            await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Er is iets misgegaan",
                detail: "De aanvraag kon niet worden verwerkt. Probeer het later opnieuw.",
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
                .ExecuteAsync(context);
        }
    }
}
