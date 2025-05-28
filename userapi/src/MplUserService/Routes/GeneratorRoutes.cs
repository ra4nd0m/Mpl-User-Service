namespace MplUserService.Routes
{
    public static class GeneratorRoutes
    {
        public static void MapGeneratorRoutes(this WebApplication app)
        {
            app.MapPost("/generator/spreadsheet/{**catchAll}", async (IHttpClientFactory httpClientFactory, HttpContext context, string catchAll, ILogger<Program> logger) =>

            {
                var client = httpClientFactory.CreateClient("SpreadsheetApi");
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
            });
        }
    }
}