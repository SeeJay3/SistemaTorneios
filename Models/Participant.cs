using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Models
{
    public class Participant
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nome do jogador é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        [Display(Name = "Nome do Jogador")]
        public string PlayerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome no jogo é obrigatório")]
        [StringLength(50, ErrorMessage = "Nome no jogo deve ter no máximo 50 caracteres")]
        [Display(Name = "Nome no Jogo")]
        public string GameUsername { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Ranking")]
        public string? Rank { get; set; }

        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;

        public int TournamentId { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        public int Points { get; set; } = 0;

        public int Position { get; set; } = 0;

        // NOVO: Dados extras da API
        [StringLength(500)]
        public string? ApiData { get; set; }

        public virtual Tournament Tournament { get; set; } = null!;
    }
}