using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Services;
using TournamentSystem.Models;

namespace TournamentSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly TournamentService _tournamentService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(TournamentService tournamentService, ILogger<HomeController> logger)
        {
            _tournamentService = tournamentService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var tournaments = await _tournamentService.GetActiveTournamentsAsync();
                return View(tournaments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar torneios na página inicial");
                return View(new List<Tournament>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}