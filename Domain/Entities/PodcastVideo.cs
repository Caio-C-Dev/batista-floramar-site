namespace BatistaFloramar.Domain.Entities
{
    public class PodcastVideo
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string YoutubeVideoId { get; set; } = string.Empty;
        public int? StartSeconds { get; set; }
        public bool Ativo { get; set; } = true;
        public int Ordem { get; set; }
        public DateTime DataPublicacao { get; set; } = DateTime.UtcNow;

        public string EmbedUrl =>
            $"https://www.youtube.com/embed/{YoutubeVideoId}{(StartSeconds.HasValue ? $"?start={StartSeconds}" : "")}";

        public string WatchUrl =>
            $"https://www.youtube.com/watch?v={YoutubeVideoId}{(StartSeconds.HasValue ? $"&t={StartSeconds}s" : "")}";
    }
}
