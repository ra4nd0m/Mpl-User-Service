using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace MplUserService.Routes
{
    public static class MaterialRoutes
    {
        public static void MapMaterialRoutes(this WebApplication app)
        {
            var dbApiBaseUrl = app.Configuration["DBApi:BaseUrl"];

            app.MapGet("/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                var client = httpClientFactory.CreateClient();
                var requestUrl = $"{dbApiBaseUrl}/{catchAll}{context.Request.QueryString}";

                var response = await client.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("GET request failed for {RequestUrl}", requestUrl);
                    return Results.Problem($"GET request failed for {requestUrl}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            });

            app.MapPost("/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                var client = httpClientFactory.CreateClient();
                var requestUrl = $"{dbApiBaseUrl}/{catchAll}{context.Request.QueryString}";

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StreamContent(context.Request.Body)
                };

                foreach (var header in context.Request.Headers)
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                var response = await client.SendAsync(requestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("POST request failed for {RequestUrl}", requestUrl);
                    return Results.Problem($"POST request failed for {requestUrl}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            });
        }
    }
}