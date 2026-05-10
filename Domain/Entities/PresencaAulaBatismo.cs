namespace BatistaFloramar.Domain.Entities
{
    public class PresencaAulaBatismo
    {
        public int Id { get; set; }
        public int AulaBatismoId { get; set; }
        public string NomePessoa { get; set; } = string.Empty;
        public bool Presente { get; set; } = true;
        public string? Observacao { get; set; }
        public AulaBatismo Aula { get; set; } = null!;
    }
}
