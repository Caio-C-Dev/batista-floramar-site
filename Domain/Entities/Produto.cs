namespace BatistaFloramar.Domain.Entities
{
    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public string? Imagem { get; set; }
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
