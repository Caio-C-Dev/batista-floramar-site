using System.ComponentModel.DataAnnotations;

namespace BatistaFloramar.Models
{
    public class BatismoViewModel
    {
        [Required(ErrorMessage = "Informe seu nome completo.")]
        [MaxLength(150, ErrorMessage = "Nome muito longo.")]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu WhatsApp.")]
        [MaxLength(30, ErrorMessage = "WhatsApp inválido.")]
        [Display(Name = "WhatsApp")]
        public string WhatsApp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(180)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o tipo de interesse.")]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = "Batismo";

        [MaxLength(1000, ErrorMessage = "Mensagem muito longa.")]
        [Display(Name = "Mensagem (opcional)")]
        public string? Mensagem { get; set; }
    }
}
