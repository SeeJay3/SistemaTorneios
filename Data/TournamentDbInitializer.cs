using TournamentSystem.Models;

namespace TournamentSystem.Data
{
    public static class TournamentDbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            try
            {
                context.Database.EnsureCreated();

                if (context.Tournaments.Any())
                {
                    return;
                }

                var tournaments = new Tournament[]
                {
                    new Tournament
                    {
                        Name = "Campeonato LoL 2025",
                        Description = "Torneio de League of Legends",
                        Game = GameType.LeagueOfLegends,
                        StartDate = DateTime.Now.AddDays(7),
                        EndDate = DateTime.Now.AddDays(14),
                        MaxParticipants = 16,
                        Prize = 1000,
                        CreatedBy = "system"
                    }
                };

                context.Tournaments.AddRange(tournaments);
                context.SaveChanges();
            }
            catch (Exception)
            {
                // Log error but don't crash
            }
        }
    }
}