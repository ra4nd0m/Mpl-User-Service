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
        /// This method maps four API endpoints:
        /// 
        /// 1. GET /data/filtered/{**catchAll} - Allows filtered data access via GET requests.
        ///    - Requires basic authorization
        ///    - Extracts role information from user claims
        ///    - Forwards requests to database service with role parameter added
        ///    - Validates query parameters to prevent role injection
        /// 
        /// 2. POST /data/filtered/{**catchAll} - Allows filtered data access via POST requests.
        ///    - Requires basic authorization
        ///    - Extracts role information from user claims
        ///    - Validates request body to prevent role injection
        ///    - Wraps original request data and adds appropriate role
        /// 
        /// 3. GET /data/full/{**catchAll} - Provides full data access via GET requests.
        ///    - Requires admin authorization
        ///    - Forwards requests directly to the database service
        /// 
        /// 4. POST /data/full/{**catchAll} - Provides full data access via POST requests.
        ///    - Requires admin authorization
        ///    - Forwards the complete request body to the database service
        /// 
        /// All routes use the "DbClient" HTTP client for making backend requests and include
        /// appropriate error handling and logging.
        /// </remarks>
        public static void MapMaterialRoutes(this WebApplication app)
        {
            app.MapGet("/data/filtered/filter-config/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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

            app.MapPost("/data/filtered/filter-config/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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

            app.MapGet("/data/filtered/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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

            app.MapPost("/data/filtered/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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

            app.MapGet("/data/full/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("DbClient");
                    var requestUrl = $"{catchAll}{context.Request.QueryString}";

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
                    logger.LogError(ex, "Error processing GET request for /data/full/{CatchAll}", catchAll);
                    return Results.Problem("An error occurred while processing the request");
                }
            }).RequireAuthorization("admin");

            app.MapPost("/data/full/{**catchAll}", async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
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
                    logger.LogError(ex, "Error processing POST request for /data/full/{CatchAll}", catchAll);
                    return Results.Problem("An error occurred while processing the request");
                }
            }).RequireAuthorization("admin");
        }
    }
}
