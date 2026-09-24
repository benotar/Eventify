using System.Text.Json;
using Eventify.SharedKernel.Domain;
using Eventify.SharedKernel.Extensions;
using Eventify.SharedKernel.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace Eventify.ServiceDefaults.Middleware;

public class IdempotencyMiddleware : IMiddleware
{
    private readonly IdempotencyOptions _idempotencyOptions;
    private readonly IDistributedCache _cache;

    public IdempotencyMiddleware(IdempotencyOptions idempotencyOptions, IDistributedCache cache)
    {
        _idempotencyOptions = idempotencyOptions;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;

        if (!_idempotencyOptions.Methods.Contains(request.Method, StringComparer.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(_idempotencyOptions.HeaderName, out var keyValues))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Invalid or missing {_idempotencyOptions.HeaderName} header");
            return;
        }

        var key = keyValues.ToString().Trim();

        if (key.IsBlank)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"{_idempotencyOptions.HeaderName} cannot be blank");
            return;
        }

        var path = request.Path.ToString();

        var existing = await GetRecordAsync(context, key);

        if (existing is not null)
        {
            await WriteStoredResponse(context, existing);
            return;
        }

        var originalBody = context.Response.Body;
        await using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        try
        {
            await next(context);

            memStream.Position = 0;
            var bodyBytes = memStream.ToArray();
            var status = context.Response.StatusCode;
            var contentType = context.Response.ContentType;

            var record = new IdempotentRequest
            {
                Key = key,
                Method = request.Method,
                Path = path,
                RequestHash = "",
                StatusCode = status,
                ContentType = contentType,
                Body = bodyBytes,
                CreatedAtUtc = DateTime.UtcNow,
            };

            try
            {
                await _cache.SetStringAsync(key,
                    JsonSerializer.Serialize(record),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _idempotencyOptions.Ttl },
                    context.RequestAborted);
            }
            catch (ArgumentNullException)
            {
                var stored = await GetRecordAsync(context, key);

                if (stored is not null)
                {
                    await WriteStoredResponse(context, stored);
                    return;
                }
            }

            memStream.Position = 0;
            await memStream.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private async Task<IdempotentRequest?> GetRecordAsync(HttpContext context, string key)
    {
        var storedRaw = await _cache.GetStringAsync(key, context.RequestAborted);
        return storedRaw!.IsNotEmpty
            ? JsonSerializer.Deserialize<IdempotentRequest>(storedRaw)
            : null;
    }

    private static async Task WriteStoredResponse(HttpContext context, IdempotentRequest stored)
    {
        context.Response.StatusCode = stored.StatusCode;

        if (stored.ContentType!.IsNotEmpty)
        {
            context.Response.ContentType = stored.ContentType;
        }

        if (stored.Body is { Length: > 0 })
        {
            await context.Response.Body.WriteAsync(stored.Body.AsMemory(0, stored.Body.Length), context.RequestAborted);
        }
    }
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
