using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace BatistaFloramar.Infrastructure
{
    public static class ImageOptimizer
    {
        /// <summary>
        /// Resizes the uploaded image to fit within maxWidth × maxHeight (preserving aspect ratio,
        /// only downscaling) and re-encodes as JPEG (quality 82) to a deterministic .jpg path.
        /// Always returns a normalised JPEG suitable for og:image previews.
        /// </summary>
        public static async Task<string> SaveOptimizedAsync(
            IFormFile file,
            string targetFolder,
            int maxWidth  = 1600,
            int maxHeight = 1200,
            int quality   = 82)
        {
            Directory.CreateDirectory(targetFolder);
            var fileName = $"{Guid.NewGuid()}.jpg";
            var fullPath = Path.Combine(targetFolder, fileName);

            using (var stream = file.OpenReadStream())
            using (var image  = await Image.LoadAsync(stream))
            {
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxWidth, maxHeight),
                        Mode = ResizeMode.Max
                    }));
                }

                var encoder = new JpegEncoder { Quality = quality };
                await image.SaveAsync(fullPath, encoder);
            }

            return fileName;
        }
    }
}
