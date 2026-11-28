using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Server.Data;
using Server.Repositories;
using Server.Services;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRegisteredRepositories();
            builder.Services.AddRegisteredServices();
            
            // Locally via dotnet user-secrets, in Docker via ConnectionStrings__DefaultConnection (see README).
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is missing. Set it locally with: " +
                    "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=localhost,1433;...\"");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddHostedService<DevelopmentDataSeeder>();
            }

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

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
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
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            app.UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            // Swagger in Development and Staging (acceptance), not in Production.
            if (!app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("Client");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
