namespace App.Web.Middleware;

/// <summary>
/// Middleware to add security headers including camera access permissions for photo uploads
/// Implements OWASP security best practices while enabling camera functionality
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers including camera access permissions
    /// </summary>
    /// <param name="context">The HTTP context</param>
    public Task Invoke(HttpContext context)
    {
        // Content Security Policy with camera support for photo uploads
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://maps.googleapis.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "img-src 'self' data: blob: https://*.googleapis.com https://*.gstatic.com; " +
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

        return _next(context);
    }
}