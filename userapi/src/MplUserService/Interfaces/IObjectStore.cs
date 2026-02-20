namespace MplUserService.Interfaces
{
    public interface IObjectStore
    {
        Task PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken ct = default
        );

        Task<(Stream Stream, string ContentType)> GetAsync(
            string key,
            CancellationToken ct = default
        );

        Task DeleteAsync(
            string key,
            CancellationToken ct = default
        );
    }
}