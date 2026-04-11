using BatistaFloramar.Application.DTOs;
using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Application.Services
{
    public class FinanceiroService
    {
        private readonly BatistaFloramarDbContext _db;

        public FinanceiroService(BatistaFloramarDbContext db) => _db = db;

        public async Task<DashboardFinanceiroDto> GetDashboardAsync(int mes, int ano)
        {
            var inicioMes = new DateTime(ano, mes, 1);
            var fimMes = inicioMes.AddMonths(1).AddTicks(-1);
            var inicioAno = new DateTime(ano, 1, 1);
            var fimAno = new DateTime(ano, 12, 31, 23, 59, 59);

            var entradasMes = await _db.EntradasFinanceiras
                .Where(e => e.Data >= inicioMes && e.Data <= fimMes)
                .ToListAsync();

            var saidasMes = await _db.SaidasFinanceiras
                .Where(s => s.Data >= inicioMes && s.Data <= fimMes)
                .ToListAsync();

            var totalEntradasAno = await _db.EntradasFinanceiras
                .Where(e => e.Data >= inicioAno && e.Data <= fimAno)
                .SumAsync(e => (decimal?)e.Valor) ?? 0;

            var totalSaidasAno = await _db.SaidasFinanceiras
                .Where(s => s.Data >= inicioAno && s.Data <= fimAno)
                .SumAsync(s => (decimal?)s.Valor) ?? 0;

            // Últimos 6 meses para gráfico
            var ultimos6 = new List<GraficoMensalDto>();
            for (int i = 5; i >= 0; i--)
            {
                var refDate = new DateTime(ano, mes, 1).AddMonths(-i);
                var ini = new DateTime(refDate.Year, refDate.Month, 1);
                var fim = ini.AddMonths(1).AddTicks(-1);

                var ent = await _db.EntradasFinanceiras
                    .Where(e => e.Data >= ini && e.Data <= fim)
                    .SumAsync(e => (decimal?)e.Valor) ?? 0;

                var sai = await _db.SaidasFinanceiras
                    .Where(s => s.Data >= ini && s.Data <= fim)
                    .SumAsync(s => (decimal?)s.Valor) ?? 0;

                ultimos6.Add(new GraficoMensalDto
                {
                    Mes = refDate.ToString("MMM/yy"),
                    Entradas = ent,
                    Saidas = sai
                });
            }

            // Entradas por tipo no mês
            var entradasPorTipo = entradasMes
                .GroupBy(e => e.Tipo.ToString())
                .ToDictionary(g => TipoEntradaLabel(g.Key), g => g.Sum(e => e.Valor));

            // Saídas por tipo no mês
            var saidasPorTipo = saidasMes
                .GroupBy(s => s.Tipo.ToString())
                .ToDictionary(g => TipoSaidaLabel(g.Key), g => g.Sum(s => s.Valor));

            // Últimas movimentações
            var ultimasEntradas = await _db.EntradasFinanceiras
                .Include(e => e.Ministerio)
                .OrderByDescending(e => e.Data)
                .Take(5)
                .ToListAsync();

            var ultimasSaidas = await _db.SaidasFinanceiras
                .OrderByDescending(s => s.Data)
                .Take(5)
                .ToListAsync();

            return new DashboardFinanceiroDto
            {
                TotalEntradasMes = entradasMes.Sum(e => e.Valor),
                TotalSaidasMes = saidasMes.Sum(s => s.Valor),
                TotalEntradasAno = totalEntradasAno,
                TotalSaidasAno = totalSaidasAno,
                MesReferencia = mes,
                AnoReferencia = ano,
                UltimosSeisMeses = ultimos6,
                EntradasPorTipo = entradasPorTipo,
                SaidasPorTipo = saidasPorTipo,
                UltimasEntradas = ultimasEntradas,
                UltimasSaidas = ultimasSaidas
            };
        }

        public async Task RegistrarEntradaAsync(EntradaFinanceira entrada)
        {
            if (entrada.Valor <= 0) throw new ArgumentException("Valor deve ser maior que zero.");
            _db.EntradasFinanceiras.Add(entrada);
            await _db.SaveChangesAsync();
        }

        public async Task RegistrarSaidaAsync(SaidaFinanceira saida)
        {
            if (saida.Valor <= 0) throw new ArgumentException("Valor deve ser maior que zero.");
            _db.SaidasFinanceiras.Add(saida);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExcluirEntradaAsync(int id)
        {
            var entrada = await _db.EntradasFinanceiras.FindAsync(id);
            if (entrada == null) return false;
            _db.EntradasFinanceiras.Remove(entrada);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExcluirSaidaAsync(int id)
        {
            var saida = await _db.SaidasFinanceiras.FindAsync(id);
            if (saida == null) return false;
            _db.SaidasFinanceiras.Remove(saida);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(List<EntradaFinanceira> Itens, decimal Total)> ListarEntradasAsync(int? mes, int? ano, TipoEntrada? tipo, int? ministerioId)
        {
            var q = _db.EntradasFinanceiras.Include(e => e.Ministerio).AsQueryable();
            if (mes.HasValue && ano.HasValue)
            {
                var ini = new DateTime(ano.Value, mes.Value, 1);
                var fim = ini.AddMonths(1).AddTicks(-1);
                q = q.Where(e => e.Data >= ini && e.Data <= fim);
            }
            else if (ano.HasValue)
            {
                q = q.Where(e => e.Data.Year == ano.Value);
            }
            if (tipo.HasValue) q = q.Where(e => e.Tipo == tipo.Value);
            if (ministerioId.HasValue) q = q.Where(e => e.MinisterioId == ministerioId.Value);
            var lista = await q.OrderByDescending(e => e.Data).ToListAsync();
            return (lista, lista.Sum(e => e.Valor));
        }

        public async Task<(List<SaidaFinanceira> Itens, decimal Total)> ListarSaidasAsync(int? mes, int? ano, TipoSaida? tipo)
        {
            var q = _db.SaidasFinanceiras.AsQueryable();
            if (mes.HasValue && ano.HasValue)
            {
                var ini = new DateTime(ano.Value, mes.Value, 1);
                var fim = ini.AddMonths(1).AddTicks(-1);
                q = q.Where(s => s.Data >= ini && s.Data <= fim);
            }
            else if (ano.HasValue)
            {
                q = q.Where(s => s.Data.Year == ano.Value);
            }
            if (tipo.HasValue) q = q.Where(s => s.Tipo == tipo.Value);
            var lista = await q.OrderByDescending(s => s.Data).ToListAsync();
            return (lista, lista.Sum(s => s.Valor));
        }

        public static string TipoEntradaLabel(string tipo) => tipo switch
        {
            "Dizimo" => "Dízimo",
            "Oferta" => "Oferta",
            "OfertaEspecifica" => "Oferta Específica",
            "Doacao" => "Doação",
            _ => tipo
        };

        public static string TipoSaidaLabel(string tipo) => tipo switch
        {
            "Aluguel" => "Aluguel",
            "Energia" => "Energia",
            "Agua" => "Água",
            "Manutencao" => "Manutenção",
            "Missoes" => "Missões",
            "Salario" => "Salário",
            "Outros" => "Outros",
            _ => tipo
        };
    }
}
