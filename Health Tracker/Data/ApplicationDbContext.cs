using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Health_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Health_Tracker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options) :base(options)
        {
            
        }

        public DbSet<WellnessEntry> WellnessEntries { get; set; }
    }
}
