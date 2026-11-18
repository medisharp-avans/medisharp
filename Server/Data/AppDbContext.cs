using Microsoft.EntityFrameworkCore;

namespace Server.Data
{
    /// <summary>
    /// EF Core database context for MediSharp. Add the DbSets for the entities here,
    /// then create a migration with: dotnet ef migrations add &lt;Name&gt;
    /// </summary>
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
    }
}
