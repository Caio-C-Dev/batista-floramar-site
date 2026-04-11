using System.ComponentModel.DataAnnotations;

namespace BatistaFloramar.Models
{
    public class PerguntaPastorViewModel
    {
        [Required(ErrorMessage = "Informe seu nome completo.")]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu telefone.")]
        [Display(Name = "Telefone")]
        public string Telefone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o assunto da pergunta.")]
        [Display(Name = "Assunto")]
        public string Assunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escreva sua pergunta ao pastor.")]
        [StringLength(1000, ErrorMessage = "A pergunta deve ter no máximo 1000 caracteres.")]
        [Display(Name = "Pergunta")]
        public string Pergunta { get; set; } = string.Empty;
    }
}
