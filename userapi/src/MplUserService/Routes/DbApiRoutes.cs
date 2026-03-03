using Microsoft.AspNetCore.Mvc;

namespace MplUserService.Routes
{
    public static class MaterialRoutes
    {
        /// <summary>
        /// Configures routes related to material data access in the application.
        /// </summary>
        /// <param name="app">The WebApplication to which routes will be added</param>
        /// <remarks>
        /// This method maps API endpoints for proxying requests to the database service:
        /// 
        /// - GET/POST /data/filter-config/{**catchAll} - Admin-only filter configuration
        /// - GET/POST /data/{**catchAll} - General data access with JWT authorization
        /// 
        /// All routes forward the Authorization header to DbApi for JWT verification.
        /// </remarks>
        public static void MapMaterialRoutes(this WebApplication app)
        {
            app.MapGet("/data/filter-config/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("DbClient");
                    var requestUrl = $"filter-config/{catchAll}{context.Request.QueryString}";

                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                    // Forward Authorization header
                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
                    }

                    var response = await client.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("GET request failed for {RequestUrl}", requestUrl);
                        return Results.Problem($"GET request failed for {requestUrl}");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing GET request for filter-config/{CatchAll}", catchAll);
                    return Results.Problem("An error occurred while processing the request");
                }
            }).RequireAuthorization("RequireAdmin");

            app.MapPost("/data/filter-config/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("DbClient");
                    var requestUrl = $"filter-config/{catchAll}{context.Request.QueryString}";

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StreamContent(context.Request.Body)
                    };

                    // Forward Authorization header
                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
                    }

                    // Copy content headers
                    foreach (var header in context.Request.Headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
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
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing POST request for filter-config/{CatchAll}", catchAll);
                    return Results.Problem("An error occurred while processing the request");
                }
            }).RequireAuthorization("RequireAdmin");

            app.MapGet("/data/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                var requestUrl = $"{catchAll}{context.Request.QueryString}";
                try
                {
                    var client = httpClientFactory.CreateClient("DbClient");

                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                    // Forward Authorization header
                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
                    }

                    var response = await client.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogWarning("GET request failed for {RequestUrl}", requestUrl);
                        return Results.Problem($"GET request failed for {requestUrl}");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    return Results.Content(content, response.Content.Headers.ContentType?.ToString() ?? "application/json");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing GET request for {RequestUrl}", requestUrl);
                    return Results.Problem("An error occurred while processing the request");
                }
            }).RequireAuthorization();

            app.MapPost("/data/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("DbClient");
                    var requestUrl = $"{catchAll}{context.Request.QueryString}";

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StreamContent(context.Request.Body)
                    };

                    // Forward Authorization header
                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
                    }

                    // Copy content headers
                    foreach (var header in context.Request.Headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                            header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
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
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing POST request for {CatchAll}", catchAll);
                    return Results.Problem("An error occurred while processing the request");
                }
            }).RequireAuthorization();
        }
    }
}
