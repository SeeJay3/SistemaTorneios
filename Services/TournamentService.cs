using Microsoft.EntityFrameworkCore;
using TournamentSystem.Data;
using TournamentSystem.Models;

namespace TournamentSystem.Services
{
    public class TournamentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TournamentService> _logger;

        public TournamentService(ApplicationDbContext context, ILogger<TournamentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Tournament>> GetActiveTournamentsAsync()
        {
            try
            {
                return await _context.Tournaments
                    .Include(t => t.Participants)
                    .Where(t => t.Status == TournamentStatus.Open || t.Status == TournamentStatus.InProgress)
                    .OrderBy(t => t.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar torneios ativos");
                return new List<Tournament>();
            }
        }

        public async Task<Tournament?> GetTournamentByIdAsync(int id)
        {
            try
            {
                return await _context.Tournaments
                    .Include(t => t.Participants)
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar torneio com ID {id}");
                return null;
            }
        }

        public async Task<bool> CreateTournamentAsync(Tournament tournament)
        {
            try
            {
                tournament.CreatedAt = DateTime.Now;
                tournament.Status = TournamentStatus.Open;

                _context.Tournaments.Add(tournament);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Torneio criado com sucesso: {tournament.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao criar torneio: {tournament.Name}");
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> JoinTournamentAsync(Participant participant)
        {
            try
            {
                var tournament = await GetTournamentByIdAsync(participant.TournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Torneio não encontrado: {participant.TournamentId}");
                    return (false, "Torneio não encontrado.");
                }

                if (tournament.Participants.Count >= tournament.MaxParticipants)
                {
                    _logger.LogWarning($"Torneio lotado: {tournament.Name}");
                    return (false, "Torneio está lotado. Não há mais vagas disponíveis.");
                }

                // Verificar se o Riot ID já está cadastrado no torneio
                var existingParticipantByRiotId = await _context.Participants
                    .FirstOrDefaultAsync(p => p.GameUsername.ToLower() == participant.GameUsername.ToLower()
                                           && p.TournamentId == participant.TournamentId);

                if (existingParticipantByRiotId != null)
                {
                    _logger.LogWarning($"Riot ID já cadastrado no torneio: {participant.GameUsername} por {existingParticipantByRiotId.PlayerName}");
                    return (false, $"O Riot ID '{participant.GameUsername}' já está cadastrado neste torneio pelo jogador '{existingParticipantByRiotId.PlayerName}'.");
                }

                // Verificar se o nome do jogador já está cadastrado no torneio
                var existingParticipantByName = await _context.Participants
                    .FirstOrDefaultAsync(p => p.PlayerName.ToLower() == participant.PlayerName.ToLower()
                                           && p.TournamentId == participant.TournamentId);

                if (existingParticipantByName != null)
                {
                    _logger.LogWarning($"Nome do jogador já cadastrado no torneio: {participant.PlayerName}");
                    return (false, $"O nome '{participant.PlayerName}' já está cadastrado neste torneio.");
                }

                participant.RegisteredAt = DateTime.Now;
                _context.Participants.Add(participant);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Participante adicionado: {participant.PlayerName} ({participant.GameUsername}) ao torneio {tournament.Name}");
                return (true, "Participante adicionado com sucesso!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao adicionar participante {participant.PlayerName}");
                return (false, "Erro interno ao processar inscrição. Tente novamente.");
            }
        }

        public async Task<List<Tournament>> GetUserTournamentsAsync(string userId)
        {
            try
            {
                return await _context.Tournaments
                    .Include(t => t.Participants)
                    .Where(t => t.Participants.Any(p => p.UserId == userId))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar torneios do usuário {userId}");
                return new List<Tournament>();
            }
        }

        public async Task<bool> RemoveParticipantAsync(int tournamentId, string playerName)
        {
            try
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.TournamentId == tournamentId &&
                                            p.PlayerName.ToLower() == playerName.ToLower());

                if (participant == null)
                {
                    _logger.LogWarning($"Participante não encontrado: {playerName} no torneio {tournamentId}");
                    return false;
                }

                _context.Participants.Remove(participant);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Participante removido: {playerName} do torneio {tournamentId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover participante {playerName} do torneio {tournamentId}");
                return false;
            }
        }

        /// <summary>
        /// Deleta um torneio e todos os seus participantes
        /// </summary>
        public async Task<bool> DeleteTournamentAsync(int tournamentId)
        {
            try
            {
                var tournament = await _context.Tournaments
                    .Include(t => t.Participants)
                    .FirstOrDefaultAsync(t => t.Id == tournamentId);

                if (tournament == null)
                {
                    _logger.LogWarning($"Torneio não encontrado para deletar: {tournamentId}");
                    return false;
                }

                // Remove todos os participantes primeiro
                if (tournament.Participants.Any())
                {
                    _context.Participants.RemoveRange(tournament.Participants);
                    _logger.LogInformation($"Removidos {tournament.Participants.Count} participantes do torneio {tournament.Name}");
                }

                // Remove o torneio
                _context.Tournaments.Remove(tournament);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Torneio deletado com sucesso: {tournament.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao deletar torneio {tournamentId}");
                return false;
            }
        }

        public async Task<List<Tournament>> SearchTournamentsAsync(string searchTerm = "", GameType? gameFilter = null, TournamentStatus? statusFilter = null)
        {
            try
            {
                var query = _context.Tournaments.Include(t => t.Participants).AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(t => t.Name.Contains(searchTerm) ||
                                           (t.Description != null && t.Description.Contains(searchTerm)));
                }

                if (gameFilter.HasValue)
                {
                    query = query.Where(t => t.Game == gameFilter.Value);
                }

                if (statusFilter.HasValue)
                {
                    query = query.Where(t => t.Status == statusFilter.Value);
                }

                var results = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Busca realizada: '{searchTerm}' - {results.Count} resultados");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar torneios com filtros");
                return new List<Tournament>();
            }
        }

        public async Task<TournamentStats> GetTournamentStatsAsync(int tournamentId)
        {
            try
            {
                var tournament = await GetTournamentByIdAsync(tournamentId);

                if (tournament == null)
                {
                    return new TournamentStats();
                }

                var stats = new TournamentStats
                {
                    TournamentId = tournamentId,
                    TournamentName = tournament.Name,
                    TotalParticipants = tournament.Participants.Count,
                    MaxParticipants = tournament.MaxParticipants,
                    RegistrationRate = tournament.MaxParticipants > 0
                        ? (double)tournament.Participants.Count / tournament.MaxParticipants * 100
                        : 0,

                    RankDistribution = tournament.Game == GameType.LeagueOfLegends
                        ? GetRankDistribution(tournament.Participants)
                        : new Dictionary<string, int>(),

                    RecentRegistrations = tournament.Participants
                        .OrderByDescending(p => p.RegisteredAt)
                        .Take(5)
                        .Select(p => new ParticipantInfo
                        {
                            PlayerName = p.PlayerName,
                            GameUsername = p.GameUsername,
                            Rank = p.Rank ?? "Não Ranqueado",
                            RegisteredAt = p.RegisteredAt
                        })
                        .ToList(),

                    DaysUntilStart = (tournament.StartDate - DateTime.Now).Days,
                    IsStartingSoon = (tournament.StartDate - DateTime.Now).TotalDays <= 7,

                    Status = tournament.Status,
                    IsFull = tournament.Participants.Count >= tournament.MaxParticipants
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao calcular estatísticas do torneio {tournamentId}");
                return new TournamentStats();
            }
        }

        private Dictionary<string, int> GetRankDistribution(ICollection<Participant> participants)
        {
            var distribution = new Dictionary<string, int>();

            foreach (var participant in participants)
            {
                var rank = ExtractTierFromRank(participant.Rank ?? "Unranked");

                if (distribution.ContainsKey(rank))
                    distribution[rank]++;
                else
                    distribution[rank] = 1;
            }

            return distribution.OrderByDescending(kvp => kvp.Value)
                             .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private string ExtractTierFromRank(string fullRank)
        {
            if (string.IsNullOrEmpty(fullRank) || fullRank.Contains("Não") || fullRank.Contains("Unranked"))
                return "Unranked";

            var tiers = new[] { "Iron", "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master", "Grandmaster", "Challenger",
                               "Ferro", "Bronze", "Prata", "Ouro", "Platina", "Diamante", "Mestre", "Grão-Mestre", "Desafiante" };

            foreach (var tier in tiers)
            {
                if (fullRank.Contains(tier, StringComparison.OrdinalIgnoreCase))
                    return tier;
            }

            return "Other";
        }

        public async Task<int> UpdateTournamentStatusAsync()
        {
            try
            {
                var now = DateTime.Now;
                var tournamentsToUpdate = await _context.Tournaments
                    .Where(t => (t.Status == TournamentStatus.Open && t.StartDate <= now) ||
                               (t.Status == TournamentStatus.InProgress && t.EndDate <= now))
                    .ToListAsync();

                foreach (var tournament in tournamentsToUpdate)
                {
                    if (tournament.Status == TournamentStatus.Open && tournament.StartDate <= now)
                    {
                        tournament.Status = TournamentStatus.InProgress;
                    }
                    else if (tournament.Status == TournamentStatus.InProgress && tournament.EndDate <= now)
                    {
                        tournament.Status = TournamentStatus.Finished;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Status atualizado para {tournamentsToUpdate.Count} torneios");
                return tournamentsToUpdate.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status dos torneios");
                return 0;
            }
        }

        public async Task<List<Tournament>> GetTournamentsByGameAsync(GameType gameType)
        {
            try
            {
                return await _context.Tournaments
                    .Include(t => t.Participants)
                    .Where(t => t.Game == gameType)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar torneios do jogo {gameType}");
                return new List<Tournament>();
            }
        }
    }

    public class TournamentStats
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = "";
        public int TotalParticipants { get; set; }
        public int MaxParticipants { get; set; }
        public double RegistrationRate { get; set; }
        public Dictionary<string, int> RankDistribution { get; set; } = new();
        public List<ParticipantInfo> RecentRegistrations { get; set; } = new();
        public int DaysUntilStart { get; set; }
        public bool IsStartingSoon { get; set; }
        public TournamentStatus Status { get; set; }
        public bool IsFull { get; set; }
    }

    public class ParticipantInfo
    {
        public string PlayerName { get; set; } = "";
        public string GameUsername { get; set; } = "";
        public string Rank { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
    }
}