using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Attributes
{
    /// <summary>
    /// Cho phep truy cap neu co Role hoac bat ky permission nao.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireGlobalRolePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly GlobalRole _role;
        private readonly string[] _permissionCodes;

        public RequireGlobalRolePermissionAttribute(GlobalRole role, params string[] permissionCodes)
        {
            _role = role;
            _permissionCodes = permissionCodes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var permissionHelper = context.HttpContext.RequestServices.GetService(typeof(IPermissionHelper)) as IPermissionHelper;
            if (permissionHelper == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var user = context.HttpContext.User;
            var currentRole = permissionHelper.GetGlobalRole(user);

            if (currentRole == _role)
                return;

            if (permissionHelper.HasAnyPermission(user, _permissionCodes))
                return;

            context.Result = new ForbidResult();
        }
    }
}
