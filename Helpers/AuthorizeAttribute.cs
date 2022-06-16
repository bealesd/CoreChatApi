using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string Role { get; set; }
    private readonly string ADMIN = "admin";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isUserLoggedIn = context.HttpContext.Items.ContainsKey("User");
        if (!isUserLoggedIn)
        {
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        var isUserRole = context.HttpContext.Items.ContainsKey("Role");
        if (!isUserRole)
        {
            context.Result = new ContentResult()
            {
                Content = "You are unauthorized!!",
                StatusCode = 401
            };
            return;
        }

        var userRole = context.HttpContext.Items["Role"].ToString();
        var isUserAdminOrRole = userRole == Role || userRole == ADMIN;
        if (Role != null && !isUserAdminOrRole)
        {
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }
}