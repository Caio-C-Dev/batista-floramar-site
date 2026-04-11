namespace BatistaFloramar.Domain.Entities
{
    public class Ministerio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ResumoBreve { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Lideranca { get; set; } = string.Empty;
        public string? FotoLider { get; set; }
        public string? WhatsApp { get; set; }
        public string Icone { get; set; } = "fas fa-church";
        public string? Link { get; set; }
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; }
        public ICollection<MinisterioFoto> Fotos { get; set; } = new List<MinisterioFoto>();
    }
}
