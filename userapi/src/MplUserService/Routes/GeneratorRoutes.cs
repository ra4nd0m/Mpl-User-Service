namespace MplUserService.Routes
{
    public static class GeneratorRoutes
    {
        /// <summary>
        /// Maps routes for the spreadsheet generator functionality.
        /// </summary>
        /// <param name="app">The WebApplication instance to which the routes should be mapped.</param>
        /// <remarks>
        /// This method adds a POST endpoint "/generator/spreadsheet/{**catchAll}" that acts as a proxy to forward requests 
        /// to the spreadsheet service. The endpoint:
        /// - Forwards the request body, headers and query parameters to the configured spreadsheet service
        /// - Requires authorization for access
        /// - Returns the response from the spreadsheet service including status code and headers
        /// - Logs warnings for unsuccessful responses
        /// </remarks>
        public static void MapGeneratorRoutes(this WebApplication app)
        {
            app.MapPost("/generator/spreadsheet/{**catchAll}", async (IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>
            {
                var client = httpClientFactory.CreateClient("SpreadsheetClient");
                var requestUrl = $"{catchAll}{context.Request.QueryString}";

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StreamContent(context.Request.Body)
                };

                foreach (var header in context.Request.Headers)
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
                }

                if (context.Request.ContentType != null)
                {
                    requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
                }

                var response = await client.SendAsync(requestMessage);

                context.Response.StatusCode = (int)response.StatusCode;

                foreach (var header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in response.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("POST request failed for {RequestUrl}", requestUrl);
                    return Results.Problem($"POST request failed for {requestUrl}");
                }

                await response.Content.CopyToAsync(context.Response.Body);
                return Results.Empty;
            }).RequireAuthorization();
        }
    }
}