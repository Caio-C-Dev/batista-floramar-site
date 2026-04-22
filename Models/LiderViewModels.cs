using System.ComponentModel.DataAnnotations;

namespace BatistaFloramar.Models
{
    public class LiderLoginViewModel
    {
        [Required(ErrorMessage = "Informe o nome da célula.")]
        [Display(Name = "Célula")]
        public string NomeCelula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [Display(Name = "Senha")]
        public string NomeLider { get; set; } = string.Empty;
    }

    public class LiderDashboardViewModel
    {
        public int CelulaId { get; set; }
        public string NomeCelula { get; set; } = string.Empty;
        public string NomeLider { get; set; } = string.Empty;
        public List<IntegranteItemViewModel> Integrantes { get; set; } = new();
        public List<PresencaResumoViewModel> UltimasPresencas { get; set; } = new();
    }

    public class IntegranteItemViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Visitante { get; set; }
        public DateTime DataIngresso { get; set; }
        public double? PercentualPresenca { get; set; }
    }

    public class PresencaResumoViewModel
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int TotalPresentes { get; set; }
        public int TotalIntegrantes { get; set; }
    }

    public class LiderRegistrarPresencaViewModel
    {
        public int CelulaId { get; set; }

        [Required(ErrorMessage = "Selecione a data.")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Selecione o tipo de reunião.")]
        public string Tipo { get; set; } = "Normal";

        public List<IntegrantePresencaItem> Integrantes { get; set; } = new();
    }

    public class IntegrantePresencaItem
    {
        public int IntegranteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Presente { get; set; }
        public string? Justificativa { get; set; }
    }

    public class LiderAdicionarIntegranteViewModel
    {
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(150, ErrorMessage = "Nome muito longo.")]
        public string Nome { get; set; } = string.Empty;

        public bool Visitante { get; set; } = false;
    }

    public class LiderHistoricoViewModel
    {
        public int CelulaId { get; set; }
        public string NomeCelula { get; set; } = string.Empty;
        public List<PresencaDetalhadaViewModel> Presencas { get; set; } = new();
    }

    public class PresencaDetalhadaViewModel
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public List<DetalhePresencaItemViewModel> Detalhes { get; set; } = new();
    }

    public class DetalhePresencaItemViewModel
    {
        public string NomeIntegrante { get; set; } = string.Empty;
        public bool Presente { get; set; }
        public string? Justificativa { get; set; }
    }
}
