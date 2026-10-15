using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireGlobalRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly GlobalRole[] _roles;

        public RequireGlobalRoleAttribute(params GlobalRole[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var permissionHelper = context.HttpContext.RequestServices.GetService(typeof(IPermissionHelper)) as IPermissionHelper;
            if (permissionHelper == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var currentRole = permissionHelper.GetGlobalRole(context.HttpContext.User);
            if (_roles == null || _roles.Length == 0 || Array.IndexOf(_roles, currentRole) >= 0)
                return;

            context.Result = new ForbidResult();
        }
    }
}
