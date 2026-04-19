namespace BatistaFloramar.Domain.Entities
{
    public class SolicitacaoBatismo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string WhatsApp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Batismo"; // "Batismo" ou "Filiação"
        public string? Mensagem { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
        public bool Atendido { get; set; } = false;
    }
}
