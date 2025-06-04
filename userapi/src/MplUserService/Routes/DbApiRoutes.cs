using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MplUserService.Data;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace MplUserService.Routes
{
    public static class MaterialRoutes
    {
        public static void MapMaterialRoutes(this WebApplication app)
        {

            app.MapGet("/data/filtered/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                var client = httpClientFactory.CreateClient("DbClient");

                var role = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var subscription = context.User.Claims.FirstOrDefault(c => c.Type == "SubscriptionType")?.Value;
                string extractedRole = (role == "Admin") ? "Admin" : subscription ?? "Free";

                var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(context.Request.QueryString.Value);
                queryParams.Remove("role");

                var newQueryString = queryParams.Count > 0
                    ? "?" + string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value.ToString() ?? string.Empty)}"))
                    : "";

                var requestUrl = $"{catchAll}{newQueryString}{(newQueryString.Length > 0 ? "&" : "?")}role={extractedRole}";

                var response = await client.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("GET request failed for {RequestUrl}", requestUrl);
                    return Results.Problem($"GET request failed for {requestUrl}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
            }).RequireAuthorization();

            app.MapPost("/data/filtered/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, UserContext dbContext, ILogger<Program> logger) =>
            {
                var client = httpClientFactory.CreateClient("DbClient");
                var requestUrl = $"{catchAll}{context.Request.QueryString}";

                string extractedRole = "";
                var role = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (role == "Admin")
                {
                    extractedRole = "Admin";
                }
                else
                {
                    var subscriptionClaim = context.User.FindFirst("SubscriptionType");
                    if (subscriptionClaim != null)
                    {
                        extractedRole = subscriptionClaim.Value;
                    }
                    else
                    {
                        logger.LogWarning("No subscription type found for user");
                        return Results.Problem("No subscription type found for user");
                    }
                }

                var doc = await JsonDocument.ParseAsync(context.Request.Body);
                var root = doc.RootElement.Clone();
                var newDoc = new { data = root, role = extractedRole };

                var jsonContent = JsonSerializer.Serialize(newDoc);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
                };

                // Copy headers (except content-related ones that we're changing)
                foreach (var header in context.Request.Headers)
                {
                    if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) &&
                        !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
                    }
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

            app.MapGet("/data/full/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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
            }).RequireAuthorization("admin");

            app.MapPost("/data/full/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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
            }).RequireAuthorization("admin");
        }
    }
}
