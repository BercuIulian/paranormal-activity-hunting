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
        public DbSet<SessionParticipant> SessionParticipants { get; set; }
        public DbSet<SessionRule> SessionRules { get; set; }
        public DbSet<SessionLog> SessionLogs { get; set; }
        public DbSet<SessionChallenge> SessionChallenges { get; set; }
        public DbSet<RequiredEquipment> RequiredEquipment { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Session relationships
            modelBuilder.Entity<Session>()
                .HasMany(s => s.Participants)
                .WithOne(p => p.Session)
                .HasForeignKey(p => p.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasMany(s => s.Rules)
                .WithOne(r => r.Session)
                .HasForeignKey(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasMany(s => s.Logs)
                .WithOne(l => l.Session)
                .HasForeignKey(l => l.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasMany(s => s.Challenges)
                .WithOne(c => c.Session)
                .HasForeignKey(c => c.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasMany(s => s.RequiredEquipment)
                .WithOne(e => e.Session)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}