using System.ComponentModel.DataAnnotations;

namespace BatistaFloramar.Models
{
    public class UsuarioListItemViewModel
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
        public DateTime CriadoEm { get; set; }
    }

    public class CriarUsuarioViewModel
    {
        [Required(ErrorMessage = "Informe o nome de usuário.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Entre 3 e 50 caracteres.")]
        [Display(Name = "Usuário")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Compare("Senha", ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar Senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;

        [Display(Name = "Perfil (Role)")]
        [StringLength(50)]
        public string Role { get; set; } = "Admin";
    }

    public class AlterarSenhaViewModel
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a nova senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a nova senha.")]
        [DataType(DataType.Password)]
        [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem.")]
        [Display(Name = "Confirmar Nova Senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }
}
