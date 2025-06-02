using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Models
{
    public class Tournament
    {
        public int Id { get; set; }

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

        public TournamentStatus Status { get; set; } = TournamentStatus.Open;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();
    }

    public enum GameType
    {
        [Display(Name = "League of Legends")]
        LeagueOfLegends = 1,

        [Display(Name = "Valorant")]
        Valorant = 2
    }

    public enum TournamentStatus
    {
        [Display(Name = "Aberto para Inscrições")]
        Open = 1,

        [Display(Name = "Em Andamento")]
        InProgress = 2,

        [Display(Name = "Finalizado")]
        Finished = 3,

        [Display(Name = "Cancelado")]
        Cancelled = 4
    }
}