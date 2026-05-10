namespace BatistaFloramar.Domain.Entities
{
    public class BatizadoHistorico
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataBatismo { get; set; }
        public string? WhatsApp { get; set; }
        public string? Observacoes { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
