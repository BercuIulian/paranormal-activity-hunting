using Microsoft.EntityFrameworkCore;
using SessionManagement.API.Models;

namespace SessionManagement.API.Data
{
    public class SessionDbContext : DbContext
    {
        public SessionDbContext(DbContextOptions<SessionDbContext> options)
            : base(options)
        {
        }

        public DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Session>()
                .Property(s => s.ParticipantIds)
                .HasConversion(
                    v => string.Join(',', v),  // Convert List<string> to a comma-separated string
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()); // Convert back to List<string>
        }
    }
}