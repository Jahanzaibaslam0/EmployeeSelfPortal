using HRMS.Services;

namespace HRMS.Middleware;

public class LoginRequiredMiddleware
{
    private readonly RequestDelegate _next;

    public LoginRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AuthService auth, PermissionService perms)
    {
        var path = context.Request.Path.Value ?? "";

        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        if (!auth.IsLoggedIn)
        {
            context.Response.Redirect("/Login");
            return;
        }

        if (!perms.CanAccessPage(path))
        {
            context.Response.Redirect("/Home?accessDenied=1");
            return;
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        var p = path.TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(p)) return true;
        if (p == "/login") return true;
        if (p.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return true;
        if (p.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
