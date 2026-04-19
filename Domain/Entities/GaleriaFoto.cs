namespace BatistaFloramar.Domain.Entities
{
    public class GaleriaFoto
    {
        public int Id { get; set; }
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string? Legenda { get; set; }
        public int AlbumId { get; set; }
        public GaleriaAlbum Album { get; set; } = null!;
    }
}
