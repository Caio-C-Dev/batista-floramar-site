namespace BatistaFloramar.Domain.Entities
{
    public class SerieMensagem
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string PlaylistId { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemCapa { get; set; }
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; } = 0;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
