using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Models.ViewModels
{
    public class CreateTournamentViewModel
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
        [Display(Name = "Nome do Torneio")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres")]
        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Jogo é obrigatório")]
        [Display(Name = "Jogo")]
        public GameType Game { get; set; }

        [Required(ErrorMessage = "Data de início é obrigatória")]
        [Display(Name = "Data de Início")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Data de fim é obrigatória")]
        [Display(Name = "Data de Fim")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Máximo de participantes é obrigatório")]
        [Range(2, 100, ErrorMessage = "Deve ter entre 2 e 100 participantes")]
        [Display(Name = "Máximo de Participantes")]
        public int MaxParticipants { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Prêmio deve ser maior ou igual a zero")]
        [Display(Name = "Prêmio (R$)")]
        public decimal Prize { get; set; }
    }

    public class JoinTournamentViewModel
    {
        [Required(ErrorMessage = "Nome do jogador é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Jogador")]
        public string PlayerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome no jogo é obrigatório")]
        [StringLength(50, ErrorMessage = "Nome no jogo deve ter no máximo 50 caracteres")]
        [Display(Name = "Nome no Jogo")]
        public string GameUsername { get; set; } = string.Empty;

        public int TournamentId { get; set; }

        public string TournamentName { get; set; } = string.Empty;

        public GameType GameType { get; set; }
    }
}