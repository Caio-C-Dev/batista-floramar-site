namespace BatistaFloramar.Domain.Entities
{
    public class EventoSemanal
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty; // "Segunda","Terça","Quarta","Quinta","Sexta","Sábado","Domingo"
        public string Horario { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; } = 0;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
