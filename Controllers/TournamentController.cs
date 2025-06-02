using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Models;
using TournamentSystem.Models.ViewModels;
using TournamentSystem.Services;

namespace TournamentSystem.Controllers
{
    public class TournamentController : Controller
    {
        private readonly TournamentService _tournamentService;
        private readonly RiotApiService _riotApiService;
        private readonly ILogger<TournamentController> _logger;

        public TournamentController(
            TournamentService tournamentService,
            RiotApiService riotApiService,
            ILogger<TournamentController> logger)
        {
            _tournamentService = tournamentService;
            _riotApiService = riotApiService;
            _logger = logger;
        }

        /// <summary>
        /// GET: Tournament
        /// Exibe lista de todos os torneios
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var tournaments = await _tournamentService.GetActiveTournamentsAsync();
                return View(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar lista de torneios");
                return View(new List<Tournament>());
            }
        }

        /// <summary>
        /// GET: Tournament/Details/5
        /// Exibe detalhes de um torneio específico
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var tournament = await _tournamentService.GetTournamentByIdAsync(id);
                if (tournament == null)
                    return NotFound();

                return View(tournament);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar detalhes do torneio {id}");
                return NotFound();
            }
        }

        /// <summary>
        /// GET: Tournament/Create
        /// Exibe formulário para criar novo torneio
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// POST: Tournament/Create
        /// Processa criação de novo torneio
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTournamentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tournament = new Tournament
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Game = model.Game,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        MaxParticipants = model.MaxParticipants,
                        Prize = model.Prize,
                        CreatedBy = "Organizador" // Como não há login, usar valor fixo
                    };

                    var success = await _tournamentService.CreateTournamentAsync(tournament);
                    if (success)
                    {
                        TempData["Success"] = "Torneio criado com sucesso!";
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError("", "Erro ao criar torneio. Tente novamente.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar torneio");
                    ModelState.AddModelError("", "Erro interno. Tente novamente.");
                }
            }

            return View(model);
        }

        /// <summary>
        /// GET: Tournament/Join/5
        /// Exibe formulário para participar de um torneio
        /// </summary>
        public async Task<IActionResult> Join(int id)
        {
            try
            {
                var tournament = await _tournamentService.GetTournamentByIdAsync(id);
                if (tournament == null)
                    return NotFound();

                var model = new JoinTournamentViewModel
                {
                    TournamentId = id,
                    TournamentName = tournament.Name,
                    GameType = tournament.Game
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar página de inscrição para torneio {id}");
                return NotFound();
            }
        }

        /// <summary>
        /// POST: Tournament/Join
        /// Processa inscrição em um torneio
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinTournamentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var tournament = await _tournamentService.GetTournamentByIdAsync(model.TournamentId);
                    if (tournament == null)
                    {
                        ModelState.AddModelError("", "Torneio não encontrado.");
                        return View(model);
                    }

                    // Usar a API da Riot Games para verificar o jogador
                    PlayerInfo playerInfo;

                    if (tournament.Game == GameType.LeagueOfLegends)
                    {
                        playerInfo = await _riotApiService.GetLeagueOfLegendsPlayerAsync(model.GameUsername);
                    }
                    else
                    {
                        playerInfo = await _riotApiService.GetValorantPlayerAsync(model.GameUsername);
                    }

                    if (!playerInfo.IsValid)
                    {
                        ModelState.AddModelError("GameUsername", playerInfo.ErrorMessage);
                        return View(model);
                    }

                    var participant = new Participant
                    {
                        PlayerName = model.PlayerName,
                        GameUsername = model.GameUsername,
                        Rank = playerInfo.FormattedRank,
                        UserId = model.PlayerName, // Usar nome do jogador como ID
                        TournamentId = model.TournamentId,
                        ApiData = $"Level: {playerInfo.Level} | WR: {playerInfo.WinRate} | LP: {playerInfo.LeaguePoints}"
                    };

                    var result = await _tournamentService.JoinTournamentAsync(participant);
                    if (result.Success)
                    {
                        // Mensagem mais limpa e visível
                        TempData["Success"] = "Inscrição realizada com sucesso!";
                        return RedirectToAction("Details", new { id = model.TournamentId });
                    }
                    else
                    {
                        // Mostrar mensagem de erro específica
                        ModelState.AddModelError("", result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar inscrição no torneio");
                    ModelState.AddModelError("", "Erro interno. Tente novamente.");
                }
            }

            return View(model);
        }

        /// <summary>
        /// GET: Tournament/MyTournaments
        /// Exibe todos os torneios (substituindo "Meus Torneios" já que não há login)
        /// </summary>
        public async Task<IActionResult> MyTournaments()
        {
            try
            {
                var tournaments = await _tournamentService.GetActiveTournamentsAsync();
                return View(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar lista de torneios");
                return View(new List<Tournament>());
            }
        }

        /// <summary>
        /// POST: Tournament/CheckPlayer
        /// API endpoint para verificar jogador via Riot Games API (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckPlayer(string username, GameType gameType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Nome de usuário é obrigatório"
                    });
                }

                PlayerInfo playerInfo;

                if (gameType == GameType.LeagueOfLegends)
                {
                    playerInfo = await _riotApiService.GetLeagueOfLegendsPlayerAsync(username.Trim());
                }
                else
                {
                    playerInfo = await _riotApiService.GetValorantPlayerAsync(username.Trim());
                }

                return Json(new
                {
                    success = playerInfo.IsValid,
                    message = playerInfo.IsValid ? "Jogador encontrado!" : playerInfo.ErrorMessage,
                    data = playerInfo.IsValid ? new
                    {
                        rank = playerInfo.FormattedRank,
                        level = playerInfo.Level,
                        winRate = playerInfo.WinRate,
                        wins = playerInfo.Wins,
                        losses = playerInfo.Losses
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar jogador {username}");
                return Json(new
                {
                    success = false,
                    message = "Erro ao verificar jogador. Tente novamente."
                });
            }
        }

        /// <summary>
        /// POST: Tournament/GetDetailedPlayerInfo
        /// API endpoint para buscar informações detalhadas via Riot Games API (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetDetailedPlayerInfo(string username, GameType gameType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Nome de usuário é obrigatório" });
                }

                var completeInfo = await _riotApiService.GetCompletePlayerInfoAsync(username.Trim(), gameType);

                if (!completeInfo.IsValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = completeInfo.ErrorMessage
                    });
                }

                if (gameType == GameType.LeagueOfLegends)
                {
                    var soloRank = completeInfo.SoloQueueRank;
                    var flexRank = completeInfo.FlexQueueRank;

                    return Json(new
                    {
                        success = true,
                        message = "Jogador encontrado!",
                        data = new
                        {
                            // Informações Básicas
                            summonerName = completeInfo.SummonerInfo?.Name,
                            level = completeInfo.SummonerInfo?.SummonerLevel ?? 0,
                            riotId = completeInfo.RiotAccount.FullRiotId,

                            // Solo Queue Rank
                            soloRank = soloRank?.FormattedRank ?? "Unranked",
                            soloTier = soloRank?.Tier ?? "UNRANKED",
                            soloLP = soloRank?.LeaguePoints ?? 0,
                            soloWins = soloRank?.Wins ?? 0,
                            soloLosses = soloRank?.Losses ?? 0,
                            soloWinRate = soloRank?.WinRate ?? 0,

                            // Flex Queue Rank
                            flexRank = flexRank?.FormattedRank ?? "Unranked",
                            flexTier = flexRank?.Tier ?? "UNRANKED",
                            flexLP = flexRank?.LeaguePoints ?? 0,
                            flexWins = flexRank?.Wins ?? 0,
                            flexLosses = flexRank?.Losses ?? 0,
                            flexWinRate = flexRank?.WinRate ?? 0,

                            // Badges e Status Especiais
                            badges = new List<string>
                            {
                                soloRank?.HotStreak == true ? "🔥 Hot Streak" : null,
                                soloRank?.Veteran == true ? "🏆 Veteran" : null,
                                soloRank?.FreshBlood == true ? "⭐ Fresh Blood" : null
                            }.Where(b => b != null).ToList()
                        }
                    });
                }
                else // Valorant
                {
                    return Json(new
                    {
                        success = true,
                        message = "Jogador Valorant encontrado!",
                        data = new
                        {
                            riotId = completeInfo.RiotAccount.FullRiotId,
                            gameName = completeInfo.RiotAccount.GameName,
                            tagLine = completeInfo.RiotAccount.TagLine,
                            activeShard = completeInfo.ActiveShard?.ShardName ?? "Unknown",
                            verified = true
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting detailed player info for {username}");
                return Json(new
                {
                    success = false,
                    message = "Erro ao verificar jogador. Tente novamente."
                });
            }
        }

        /// <summary>
        /// GET: Tournament/GetTournamentData/5
        /// API endpoint para buscar dados atualizados de um torneio (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTournamentData(int id)
        {
            try
            {
                var tournament = await _tournamentService.GetTournamentByIdAsync(id);
                if (tournament == null)
                {
                    return Json(new { success = false, message = "Torneio não encontrado" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = tournament.Id,
                        name = tournament.Name,
                        participantCount = tournament.Participants.Count,
                        maxParticipants = tournament.MaxParticipants,
                        status = tournament.Status.ToString(),
                        participants = tournament.Participants.Select(p => new
                        {
                            id = p.Id,
                            playerName = p.PlayerName,
                            gameUsername = p.GameUsername,
                            rank = p.Rank,
                            registeredAt = p.RegisteredAt,
                            apiData = p.ApiData
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar dados do torneio {id}");
                return Json(new { success = false, message = "Erro interno" });
            }
        }

        /// <summary>
        /// POST: Tournament/LeaveTournament
        /// Remove participante de um torneio
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LeaveTournament(int tournamentId, string playerName)
        {
            try
            {
                var success = await _tournamentService.RemoveParticipantAsync(tournamentId, playerName);

                if (success)
                {
                    TempData["Success"] = "Você saiu do torneio com sucesso!";
                }
                else
                {
                    TempData["Error"] = "Erro ao sair do torneio.";
                }

                return RedirectToAction("Details", new { id = tournamentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover participante {playerName} do torneio {tournamentId}");
                TempData["Error"] = "Erro interno. Tente novamente.";
                return RedirectToAction("Details", new { id = tournamentId });
            }
        }

        /// <summary>
        /// POST: Tournament/Delete/5
        /// Deleta um torneio e todos os seus participantes
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _tournamentService.DeleteTournamentAsync(id);

                if (success)
                {
                    TempData["Success"] = "Torneio deletado com sucesso!";
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Torneio não encontrado." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao deletar torneio {id}");
                return Json(new { success = false, message = "Erro interno. Tente novamente." });
            }
        }

        /// <summary>
        /// GET: Tournament/Search
        /// Busca torneios por critérios
        /// </summary>
        public async Task<IActionResult> Search(string searchTerm = "", GameType? gameFilter = null, TournamentStatus? statusFilter = null)
        {
            try
            {
                var tournaments = await _tournamentService.SearchTournamentsAsync(searchTerm, gameFilter, statusFilter);

                ViewBag.SearchTerm = searchTerm;
                ViewBag.GameFilter = gameFilter;
                ViewBag.StatusFilter = statusFilter;

                return View("Index", tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar torneios");
                return View("Index", new List<Tournament>());
            }
        }

        /// <summary>
        /// GET: Tournament/Stats/5
        /// Exibe estatísticas de um torneio
        /// </summary>
        public async Task<IActionResult> Stats(int id)
        {
            try
            {
                var tournament = await _tournamentService.GetTournamentByIdAsync(id);
                if (tournament == null)
                    return NotFound();

                var stats = await _tournamentService.GetTournamentStatsAsync(id);

                ViewBag.Tournament = tournament;
                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao carregar estatísticas do torneio {id}");
                return NotFound();
            }
        }
    }
}