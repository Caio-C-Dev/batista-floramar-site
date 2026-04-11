namespace BatistaFloramar.Domain.Entities
{
    public class MinisterioFoto
    {
        public int Id { get; set; }
        public int MinisterioId { get; set; }
        public Ministerio Ministerio { get; set; } = null!;
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string? Legenda { get; set; }
        public DateTime DataUpload { get; set; } = DateTime.UtcNow;
    }
}
