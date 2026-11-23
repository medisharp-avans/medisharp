using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Server.Data;
using Server.Repositories;
using Server.Filters;
using Server.Services;
using System.Reflection;

namespace Server
{
    /// <summary>Configures the API, its scoped dependencies and database startup.</summary>
    public class Program
    {
        /// <summary>Applies pending migrations when enabled and starts the HTTP server.</summary>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Locally via dotnet user-secrets, in Docker via ConnectionStrings__DefaultConnection (see README).
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is missing. Set it locally with: " +
                    "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=localhost,1433;...\"");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddRegisteredRepositories();
            builder.Services.AddRegisteredServices();
            builder.Services.AddScoped<DevelopmentOnlyFilter>();

            // HTTPS is handled by the reverse proxy; take the scheme and client IP from the X-Forwarded-* headers.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // The proxy runs in a Docker network with a changing IP, so do not restrict this to localhost.
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddControllers(options =>
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
                .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);
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
            if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
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

            app.UseAuthorization();


            app.MapControllers();

            await app.RunAsync();
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
