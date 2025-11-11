using System.Net;

namespace PokeSync.Api.Middleware;

public sealed class InternalTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expected;

    public const string HeaderName = "X-Internal-Token";

    public InternalTokenMiddleware(RequestDelegate next, IConfiguration cfg)
    {
        _next = next;
        _expected = cfg["InternalApi:Token"] ?? string.Empty;
    }

    public async Task Invoke(HttpContext context)
    {
        // N’applique la vérif que sur /internal
        if (context.Request.Path.StartsWithSegments("/internal", StringComparison.OrdinalIgnoreCase))
        {
            if (!_expected.HasValue())
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Internal token not configured.");
                return;
            }

            if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
                !string.Equals(provided.ToString(), _expected, StringComparison.Ordinal))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Invalid internal token.");
                return;
            }
        }

        await _next(context);
    }
}

file static class StringExt { public static bool HasValue(this string s) => !string.IsNullOrWhiteSpace(s); }
