using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace CoKhiThaCo.Attributes
{
    /// <summary>
    /// Xac thuc API key tu header "ApiKey" cho may-to-machine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        public const string HeaderName = "ApiKey";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var expectedKey = configuration?["ApiKey"];

            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || !string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
