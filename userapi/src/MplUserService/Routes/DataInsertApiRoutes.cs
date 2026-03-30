using System.Net.Http.Headers;

namespace MplUserService.Routes
{
    public static class DataInsertApiRoutes
    {
        private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Connection",
            "Keep-Alive",
            "Proxy-Authenticate",
            "Proxy-Authorization",
            "TE",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade"
        };

        public static void MapDataInsertApiRoutes(this WebApplication app)
        {
            app.MapPost("/data-insert/{**catchAll}", ProxyDataInsert)
               .RequireAuthorization("RequireAdmin");
        }

        private static async Task ProxyDataInsert(
           IHttpClientFactory httpClientFactory,
           HttpContext context,
           string catchAll,
           ILogger<Program> logger)
        {
            var client = httpClientFactory.CreateClient("SpreadsheetClient");

            var requestUrl = $"{catchAll}{context.Request.QueryString}";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            // Forward body (streaming) if present
            requestMessage.Content = new StreamContent(context.Request.Body);

            // Copy request headers correctly
            foreach (var header in context.Request.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key))
                    continue;

                // Content headers vs normal headers
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Ensure content type is preserved (safer explicit set)
            if (!string.IsNullOrEmpty(context.Request.ContentType))
            {
                requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }

            // Stream the response (do not buffer)
            using var responseMessage = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            context.Response.StatusCode = (int)responseMessage.StatusCode;

            // Copy response headers
            foreach (var header in responseMessage.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key))
                    continue;

                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key))
                    continue;

                // Optional: avoid setting Content-Length explicitly during streaming
                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Some servers add this automatically; avoid duplicates
            context.Response.Headers.Remove("transfer-encoding");

            if (!responseMessage.IsSuccessStatusCode)
            {
                logger.LogWarning("Spreadsheet proxy failed for {RequestUrl} with {StatusCode}", requestUrl, responseMessage.StatusCode);

                // IMPORTANT: don't try to write a Problem() AFTER copying headers for a file response.
                // Just pass-through the upstream body (it may contain useful JSON error).
            }

            await using var upstream = await responseMessage.Content.ReadAsStreamAsync(context.RequestAborted);
            await upstream.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
    }
}