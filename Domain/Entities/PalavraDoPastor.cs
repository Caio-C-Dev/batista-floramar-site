namespace BatistaFloramar.Domain.Entities
{
    public enum TipoPalavra { Sermao, Devocional }

    public class PalavraDoPastor
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public TipoPalavra Tipo { get; set; } = TipoPalavra.Devocional;
        public string AutorNome { get; set; } = string.Empty;
        public string? ImagemDestaque { get; set; }
        public bool Publicado { get; set; } = false;
        public DateTime DataPublicacao { get; set; } = DateTime.Today;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
