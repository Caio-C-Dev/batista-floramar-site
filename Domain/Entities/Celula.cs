namespace BatistaFloramar.Domain.Entities
{
    public class Celula
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Lideres { get; set; }
        /// <summary>
        /// Nome do líder usado para autenticação na Área dos Líderes.
        /// MVP: senha = primeira letra maiúscula deste valor.
        /// Futuro: substituir por hash BCrypt.
        /// </summary>
        public string? LiderNome { get; set; }
        public string? Endereco { get; set; }
        public string? Contato { get; set; }
        public string Horario { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string ImagemUrl { get; set; } = "CelulaPeregrinos.jpeg";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Navegação
        public ICollection<Integrante> Integrantes { get; set; } = new List<Integrante>();
    }
}
