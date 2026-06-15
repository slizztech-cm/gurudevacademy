using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GurudevDefenceAcademy.Middleware;

// Requires a logged-in student (session "user_email" present).
public class UserAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext ctx)
    {
        var email = ctx.HttpContext.Session.GetString("user_email");
        if (string.IsNullOrEmpty(email))
        {
            var path = ctx.HttpContext.Request.Path + ctx.HttpContext.Request.QueryString;
            ctx.Result = new RedirectResult("/account/login?returnUrl=" + Uri.EscapeDataString(path));
        }
    }
}

// Requires a logged-in admin/superadmin (session "admin_email" present).
public class AdminAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext ctx)
    {
        var role = ctx.HttpContext.Session.GetString("admin_role");
        if (role is not ("admin" or "superadmin"))
            ctx.Result = new RedirectResult("/admin/login");
    }
}
