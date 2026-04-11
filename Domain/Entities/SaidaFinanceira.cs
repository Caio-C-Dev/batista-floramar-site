namespace BatistaFloramar.Domain.Entities
{
    public enum TipoSaida
    {
        Aluguel,
        Energia,
        Agua,
        Manutencao,
        Missoes,
        Salario,
        Outros
    }

    public class SaidaFinanceira
    {
        public int Id { get; set; }
        public TipoSaida Tipo { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string? RegistradoPor { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
