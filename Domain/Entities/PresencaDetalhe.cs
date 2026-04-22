namespace BatistaFloramar.Domain.Entities
{
    public class PresencaDetalhe
    {
        public int Id { get; set; }
        public int PresencaId { get; set; }
        public Presenca Presenca { get; set; } = null!;
        public int IntegranteId { get; set; }
        public Integrante Integrante { get; set; } = null!;
        public bool Presente { get; set; }
        public string? Justificativa { get; set; }
    }
}
