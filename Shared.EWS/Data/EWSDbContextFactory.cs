using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shared.EWS.Data
{
    public class EWSDbContextFactory : IDesignTimeDbContextFactory<EWSDbContext>
    {
        public EWSDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EWSDbContext>();

            // PostgreSQL connection string
            optionsBuilder.UseNpgsql("Host=localhost;Database=EWS;Username=ewsuser;Password=Admin@123");

            return new EWSDbContext(optionsBuilder.Options);
        }
    }
}