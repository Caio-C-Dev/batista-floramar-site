namespace BatistaFloramar.Domain.Entities
{
    public enum TipoEntrada
    {
        Dizimo,
        Oferta,
        OfertaEspecifica,
        Doacao
    }

    public class EntradaFinanceira
    {
        public int Id { get; set; }
        public TipoEntrada Tipo { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string? Origem { get; set; }
        public int? MinisterioId { get; set; }
        public Ministerio? Ministerio { get; set; }
        public string? RegistradoPor { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
