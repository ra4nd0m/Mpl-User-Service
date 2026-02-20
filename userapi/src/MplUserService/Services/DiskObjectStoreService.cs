using MplUserService.Interfaces;

namespace MplUserService.Services
{
    public sealed class DiskObjectStoreService(string rootPath) : IObjectStore
    {
        public async Task PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken ct = default
        )
        {
            var path = Path.Combine(rootPath, key);

            await using var fileStream = File.Create(path);
            await content.CopyToAsync(fileStream, ct);
        }
        public Task<(Stream Stream, string ContentType)> GetAsync(string key, CancellationToken ct = default)
        {
            var path = Path.Combine(rootPath, key);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            Stream stream = File.OpenRead(path);

            return Task.FromResult<(Stream, string)>((stream, "application/pdf"));
        }
        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            var path = Path.Combine(rootPath, key);

            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }

    }
}