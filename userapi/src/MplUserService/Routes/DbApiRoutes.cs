using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace MplUserService.Routes
{
    public static class MaterialRoutes
    {
        public static void MapMaterialRoutes(this WebApplication app)
        {

            app.MapGet("/data/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                var client = httpClientFactory.CreateClient("DbClient");
                var requestUrl = $"{catchAll}{context.Request.QueryString}";

                var response = await client.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("GET request failed for {RequestUrl}", requestUrl);
                    return Results.Problem($"GET request failed for {requestUrl}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            }).RequireAuthorization();


            app.MapPost("/data/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>

            {
                var client = httpClientFactory.CreateClient("DbClient");
                var requestUrl = $"{catchAll}{context.Request.QueryString}";

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StreamContent(context.Request.Body)
                };

                foreach (var header in context.Request.Headers)
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
                }

                var response = await client.SendAsync(requestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("POST request failed for {RequestUrl}", requestUrl);
                    return Results.Problem($"POST request failed for {requestUrl}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            }).RequireAuthorization();
        }
    }
}
