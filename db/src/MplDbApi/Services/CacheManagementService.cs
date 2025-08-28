using Microsoft.Extensions.Caching.Memory;

namespace MplDbApi.Services
{
    public class CacheManagementService(ILogger<CacheManagementService> logger, IMemoryCache memoryCache)
    {
        public void ClearCache()
        {
            try
            {
                if (memoryCache is MemoryCache mc)
                {
                    mc.Clear();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing cache");
                throw new InvalidOperationException("Failed to clear cache");
            }
        }
    }
}