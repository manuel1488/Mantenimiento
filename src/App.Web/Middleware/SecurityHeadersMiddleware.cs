using App.Core.Interfaces;

using Microsoft.Extensions.Caching.Memory;

namespace App.Web.Middleware;

/// <summary>
/// Middleware to add security headers including camera access permissions for photo uploads
/// Implements OWASP security best practices while enabling camera functionality
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private const string MinioEndpointCacheKey = "SecurityHeadersMiddleware:MinioEndpointHost";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers including camera access permissions
    /// </summary>
    public async Task Invoke(HttpContext context, IMinioConfiguracionService minioConfiguracionService, IMemoryCache cache)
    {
        // The MinIO/S3 endpoint is user-configurable at runtime (stored in DB, not appsettings) —
        // it must be added to img-src dynamically so evidence photos actually render, without
        // broadening the policy to allow arbitrary https: hosts. Cached briefly since this runs on every request.
        var minioImgSrc = await cache.GetOrCreateAsync(MinioEndpointCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var config = await minioConfiguracionService.GetConfigAsync();
            if (config is null || string.IsNullOrWhiteSpace(config.Endpoint))
                return string.Empty;

            var host = config.Endpoint
                .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');
            return $"{(config.UseSsl ? "https" : "http")}://{host}";
        });

        // Content Security Policy with camera support for photo uploads
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://maps.googleapis.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            $"img-src 'self' data: blob: https://*.googleapis.com https://*.gstatic.com{(string.IsNullOrEmpty(minioImgSrc) ? "" : $" {minioImgSrc}")}; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "frame-src https://www.google.com; " +
            "connect-src 'self' https://*.googleapis.com; " +
            "media-src 'self' blob:; " +
            "camera 'self'");

        // Permissions Policy for modern browsers - controls camera and other sensitive APIs
        context.Response.Headers.Append("Permissions-Policy", 
            "camera=self, " +
            "microphone=(), " +
            "geolocation=(), " +
            "payment=()");

        
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // HSTS header for HTTPS enforcement (required for camera access)
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains");
        }

        await _next(context);
    }
}