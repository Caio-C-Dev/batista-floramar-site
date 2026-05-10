using System.ComponentModel.DataAnnotations;

namespace BatistaFloramar.Models
{
    public class AreaBatismoLoginViewModel
    {
        [Required(ErrorMessage = "Informe o usuário.")]
        [Display(Name = "Usuário")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;
    }

    public class NovaAulaViewModel
    {
        [Required(ErrorMessage = "Informe o título.")]
        [MaxLength(200)]
        [Display(Name = "Título da aula")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o número da aula.")]
        [Range(1, 99, ErrorMessage = "Número deve ser entre 1 e 99.")]
        [Display(Name = "Número da aula")]
        public int NumeroAula { get; set; }

        [Required(ErrorMessage = "Informe a data.")]
        [Display(Name = "Data da aula")]
        public DateTime DataAula { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Informe o professor.")]
        [MaxLength(150)]
        [Display(Name = "Quem deu a aula")]
        public string ProfessorNome { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }

        [Display(Name = "Presentes (um nome por linha)")]
        public string? PresentesTexto { get; set; }

        [Display(Name = "Ausentes (um nome por linha)")]
        public string? AusentesTexto { get; set; }
    }

    public class NovoBatizadoViewModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [MaxLength(150)]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a data do batismo.")]
        [Display(Name = "Data do batismo")]
        public DateTime DataBatismo { get; set; } = DateTime.Today;

        [MaxLength(30)]
        [Display(Name = "WhatsApp")]
        public string? WhatsApp { get; set; }

        [MaxLength(500)]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }
    }
}
