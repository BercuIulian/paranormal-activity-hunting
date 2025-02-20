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
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => Guid.Parse(id))
                        .ToList());
        }
    }
}