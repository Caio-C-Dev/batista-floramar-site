using BatistaFloramar.Domain.Entities;

namespace BatistaFloramar.Application.DTOs
{
    public class DashboardFinanceiroDto
    {
        public decimal TotalEntradasMes { get; set; }
        public decimal TotalSaidasMes { get; set; }
        public decimal SaldoMes => TotalEntradasMes - TotalSaidasMes;
        public decimal TotalEntradasAno { get; set; }
        public decimal TotalSaidasAno { get; set; }
        public decimal SaldoAno => TotalEntradasAno - TotalSaidasAno;
        public int MesReferencia { get; set; }
        public int AnoReferencia { get; set; }
        public List<GraficoMensalDto> UltimosSeisMeses { get; set; } = new();
        public Dictionary<string, decimal> EntradasPorTipo { get; set; } = new();
        public Dictionary<string, decimal> SaidasPorTipo { get; set; } = new();
        public List<EntradaFinanceira> UltimasEntradas { get; set; } = new();
        public List<SaidaFinanceira> UltimasSaidas { get; set; } = new();
    }

    public class GraficoMensalDto
    {
        public string Mes { get; set; } = string.Empty;
        public decimal Entradas { get; set; }
        public decimal Saidas { get; set; }
    }

    public class FiltroFinanceiroDto
    {
        public int? Mes { get; set; }
        public int? Ano { get; set; }
        public string? Tipo { get; set; }
        public int? MinisterioId { get; set; }
    }
}
