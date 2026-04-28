using System.Text.RegularExpressions;

namespace BatistaFloramar.Infrastructure
{
    public static class SlugHelper
    {
        public static string Gerar(string texto)
        {
            var s = texto.ToLowerInvariant();
            s = Regex.Replace(s, @"[àáâãäå]", "a");
            s = Regex.Replace(s, @"[èéêë]", "e");
            s = Regex.Replace(s, @"[ìíîï]", "i");
            s = Regex.Replace(s, @"[òóôõö]", "o");
            s = Regex.Replace(s, @"[ùúûü]", "u");
            s = Regex.Replace(s, @"[ç]", "c");
            s = Regex.Replace(s, @"[ñ]", "n");
            s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"-{2,}", "-");
            return s.Trim('-');
        }

        /// <summary>
        /// Ensures uniqueness by appending the record ID if the generated slug already exists.
        /// </summary>
        public static async Task<string> GerarUnicoAsync(
            string texto,
            int? idAtual,
            Func<string, int?, Task<bool>> existeAsync)
        {
            var slug = Gerar(texto);
            if (!await existeAsync(slug, idAtual))
                return slug;

            // Append numeric suffix until unique
            for (int i = 2; i < 1000; i++)
            {
                var candidate = $"{slug}-{i}";
                if (!await existeAsync(candidate, idAtual))
                    return candidate;
            }
            return $"{slug}-{Guid.NewGuid():N}";
        }
    }
}
