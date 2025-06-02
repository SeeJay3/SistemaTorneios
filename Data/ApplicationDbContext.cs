using Microsoft.EntityFrameworkCore;
using TournamentSystem.Models;

namespace TournamentSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Participant> Participants { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuração para Tournament
            builder.Entity<Tournament>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.Prize).HasColumnType("decimal(18,2)");
                entity.Property(t => t.CreatedBy).IsRequired().HasMaxLength(100);
            });

            // Configuração para Participant
            builder.Entity<Participant>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.PlayerName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.GameUsername).IsRequired().HasMaxLength(50);
                entity.Property(p => p.Rank).HasMaxLength(100);
                entity.Property(p => p.UserId).IsRequired().HasMaxLength(100);
                entity.Property(p => p.ApiData).HasMaxLength(500); // NOVO CAMPO

                entity.HasOne(p => p.Tournament)
                      .WithMany(t => t.Participants)
                      .HasForeignKey(p => p.TournamentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}