using System.Text.Json;

namespace MplUserService.Services
{
    public class DbApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DbApiService> logger)
    {
        public async Task<object> GetAllMaterials()
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("DbApi");
                var baseAddress = configuration["DbApi:BaseAddress"] ?? throw new InvalidOperationException("DbApi:BaseAddress is required");
                var endpoint = configuration["DbApi:MaterialsEndpoint"] ?? "materials";
                var url = $"{baseAddress}/{endpoint}";
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var materials = JsonSerializer.Deserialize<object>(responseContent) ?? new { };
                return materials;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get materials");
                throw;
            }
        }
        public async Task<object> GetMaterial(int id)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("DbApi");
                var baseAddress = configuration["DbApi:BaseAddress"] ?? throw new InvalidOperationException("DbApi:BaseAddress is required");
                var endpoint = configuration["DbApi:MaterialsEndpoint"] ?? "materials";
                var url = $"{baseAddress}/{endpoint}";
                var response = await httpClient.GetAsync($"{url}/{id}");
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var material = JsonSerializer.Deserialize<object>(responseContent) ?? new { };
                return material;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get material");
                throw;
            }
        }
    }
}