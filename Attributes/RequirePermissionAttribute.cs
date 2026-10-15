using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CoKhiThaCo.Helpers;

namespace CoKhiThaCo.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _permissionCodes;

        public RequirePermissionAttribute(params string[] permissionCodes)
        {
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

            try
            {
                if (permissionHelper.HasAnyPermission(context.HttpContext.User, _permissionCodes))
                    return;

                context.Result = new ForbidResult();
            }
            catch (UnauthorizedAccessException)
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
