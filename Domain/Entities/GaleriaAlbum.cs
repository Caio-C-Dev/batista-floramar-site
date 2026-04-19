namespace BatistaFloramar.Domain.Entities
{
    public class GaleriaAlbum
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime Data { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public List<GaleriaFoto> Fotos { get; set; } = new();
    }
}
