using System.Threading;
using System.Threading.Tasks;

namespace MarkdownViewer.Core.Services
{
    public interface IImageCache
    {
        /// <summary>
        /// Retrieves an image from the cache or downloads it if not cached.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<byte[]> GetImageAsync(string source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an image from the cache without downloading it.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<byte[]?> GetImageFromCacheAsync(string source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Caches an image.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="imageData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CacheImageAsync(
            string source,
            byte[] imageData,
            CancellationToken cancellationToken = default
        );
    }
}
