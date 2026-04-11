namespace BatistaFloramar.Domain.Entities
{
    public class PerguntaPastor
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Pergunta { get; set; } = string.Empty;
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    }
}