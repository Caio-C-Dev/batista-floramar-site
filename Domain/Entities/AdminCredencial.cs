namespace BatistaFloramar.Domain.Entities
{
    public class AdminCredencial
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? Role { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
