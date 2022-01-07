using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;

namespace Prospect.Server.Api.Middleware;

public class RequestLoggerMiddleware
{
    private readonly ILogger<RequestLoggerMiddleware> _logger;
    private readonly RequestDelegate _next;

    public RequestLoggerMiddleware(ILogger<RequestLoggerMiddleware> logger, RequestDelegate next)
    {
        _logger = logger;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var body = await RequestAsync(context.Request);

        if (context.Request.Method == "POST")
        {
            var requestId = "NULL";
                
            if (context.Request.Headers.TryGetValue("X-RequestID", out var requestIdValues))
            {
                requestId = requestIdValues.ToString();
            }
                
            _logger.LogDebug("URL {Url} RequestId {RequestId} Body {Body}", context.Request.GetDisplayUrl(), requestId, body);
        }
            
        await _next(context);
    }

    private static async Task<string> RequestAsync(HttpRequest request)
    {
        request.EnableBuffering();

        try
        {
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, false, 4096, true))
            {
                var body = await reader.ReadToEndAsync();
                if (body.StartsWith("{"))
                {
                    return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(body), Formatting.Indented);
                }

                return body;
            }
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}