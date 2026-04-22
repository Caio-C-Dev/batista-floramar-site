namespace BatistaFloramar.Domain.Entities
{
    public enum TipoPresenca
    {
        Normal,
        NaoHoveCelula,
        CelulaLivre
    }

    public class Presenca
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public int CelulaId { get; set; }
        public Celula Celula { get; set; } = null!;
        public TipoPresenca Tipo { get; set; } = TipoPresenca.Normal;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public ICollection<PresencaDetalhe> Detalhes { get; set; } = new List<PresencaDetalhe>();
    }
}
