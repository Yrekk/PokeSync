using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PokeSync.Infrastructure.Services;

namespace PokeSync.Api.Middleware;

public sealed class IdempotencyMiddleware

{
    private const int MaxBufferedBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> Methods = new(["POST", "PUT", "PATCH"]);
    // Optionnel: limiter à une route précise
    private const string TargetPath = "/internal/upsert/pokemons";
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        // 0) Périmètre : méthode, JSON, header présent, chemin (optionnel)
        if (!Methods.Contains(context.Request.Method))
        { await _next(context); return; }

        var contentType = context.Request.ContentType ?? "";
        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        { await _next(context); return; }

        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var key) ||
            string.IsNullOrWhiteSpace(key))
        { await _next(context); return; }

        // Optionnel : n’activer que sur un endpoint précis
        if (!context.Request.Path.StartsWithSegments(TargetPath, StringComparison.OrdinalIgnoreCase))
        { await _next(context); return; }

        // 1) Lire le body (avec garde-fou taille)
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        if (body.Length > MaxBufferedBytes)
        {
            // Trop gros: on ne tente pas l’idempotence, on laisse passer
            context.Request.Body.Position = 0;
            await _next(context);
            return;
        }
        context.Request.Body.Position = 0;

        // 2) Vérif magasin idempotence
        var (exists, samePayload, record) =
            await idempotencyService.CheckAsync(key!, body, context.RequestAborted);

        if (exists)
        {
            if (!samePayload)
            {
                var correlationId = context.Items.TryGetValue("CorrelationId", out var cidObj) ? cidObj?.ToString() : null;
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync($$"""
                {
                  "message": "Idempotency key '{{key}}' already used with a different payload.",
                  "correlationId": {{(correlationId is null ? "null" : $"\"{correlationId}\"")}}
                }
                """);
                return;
            }

            if (record?.ResponseBody is not null)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(record.ResponseBody);
                return;
            }
        }

        // 3) Capturer la réponse pour la persister si succès
        var originalBody = context.Response.Body;
        await using var memory = new MemoryStream();
        context.Response.Body = memory;

        try
        {
            await _next(context);

            memory.Position = 0;
            using var sr = new StreamReader(memory, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var responseText = await sr.ReadToEndAsync();

            memory.Position = 0;
            await memory.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;

            // 2xx → on enregistre la clé + snapshot (peut être vide en 204)
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await idempotencyService.SaveAsync(key!, body, responseText, context.RequestAborted);
            }
        }
        finally
        {
            // Toujours restaurer le flux
            context.Response.Body = originalBody;
        }
    }
}
