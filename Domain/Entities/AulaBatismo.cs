namespace BatistaFloramar.Domain.Entities
{
    public class AulaBatismo
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public int NumeroAula { get; set; }
        public DateTime DataAula { get; set; }
        public string ProfessorNome { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public ICollection<PresencaAulaBatismo> Presencas { get; set; } = new List<PresencaAulaBatismo>();
    }
}
