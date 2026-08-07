namespace BatistaFloramar.Domain.Entities
{
    public class MateriaSeminario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        /// <summary>Ano do curso: 1 ou 2.</summary>
        public int Ano { get; set; } = 1;
        /// <summary>Semestre dentro do ano: 1 ou 2.</summary>
        public int Semestre { get; set; } = 1;
        public string? Professor { get; set; }
        /// <summary>Carga horária em horas-aula.</summary>
        public int CargaHoraria { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
