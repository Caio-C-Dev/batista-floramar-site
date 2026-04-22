namespace BatistaFloramar.Domain.Entities
{
    public class Integrante
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int CelulaId { get; set; }
        public Celula Celula { get; set; } = null!;
        public bool Ativo { get; set; } = true;
        public bool Visitante { get; set; } = false;
        public DateTime DataIngresso { get; set; } = DateTime.UtcNow;

        // Navegação
        public ICollection<PresencaDetalhe> Detalhes { get; set; } = new List<PresencaDetalhe>();
    }
}
