using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace PokeSync.Infrastructure.Data
{
    // Design-time factory so `dotnet ef` can create the DbContext
    // without bootstrapping the ASP.NET host (and thus without Swagger).
    public class PokeSyncDbContextFactory : IDesignTimeDbContextFactory<PokeSyncDbContext>
    {

        public PokeSyncDbContext CreateDbContext(string[] args)
        {

            var builder = new DbContextOptionsBuilder<PokeSyncDbContext>();

            // 1) Try env var first: ConnectionStrings__Default
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var cs = config["ConnectionStrings:Default"];

            // 2) Fallback to default LocalDB if not provided
            if (string.IsNullOrWhiteSpace(cs))
            {
                cs = "Server=(localdb)\\MSSQLLocalDB;Database=PokeSync;Trusted_Connection=True;MultipleActiveResultSets=true";
            }

            builder.UseSqlServer(cs);
            return new PokeSyncDbContext(builder.Options);
        }
    }
}
