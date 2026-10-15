using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CoKhiThaCo.Data;
using CoKhiThaCo.Models.Enums;

namespace CoKhiThaCo.Helpers
{
    public interface IPermissionHelper
    {
        Guid GetCurrentUserId(ClaimsPrincipal user);
        GlobalRole GetGlobalRole(ClaimsPrincipal user);
        ApplicationUser GetCurrentUser(ClaimsPrincipal user);
        bool IsAdmin(ClaimsPrincipal user);
        bool IsManageMaterial(ClaimsPrincipal user);
        bool HasPermission(ClaimsPrincipal user, string permissionCode);
        bool HasAnyPermission(ClaimsPrincipal user, params string[] permissionCodes);
        bool HasAllPermissions(ClaimsPrincipal user, params string[] permissionCodes);
        ICollection<string> GetUserPermissions(ClaimsPrincipal user);
    }

    public class PermissionHelper : IPermissionHelper
    {
        private readonly MyDbContext _context;

        public PermissionHelper(MyDbContext context)
        {
            _context = context;
        }

        public Guid GetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User not authenticated");
            return Guid.Parse(userIdClaim.Value);
        }

        public GlobalRole GetGlobalRole(ClaimsPrincipal user)
        {
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                return GlobalRole.Member;
            return Enum.Parse<GlobalRole>(roleClaim.Value);
        }

        public ApplicationUser GetCurrentUser(ClaimsPrincipal user)
        {
            var userId = GetCurrentUserId(user);
            return _context.Users.FirstOrDefault(u => u.Id == userId && !u.IsDeleted);
        }

        public bool IsAdmin(ClaimsPrincipal user)
        {
            return GetGlobalRole(user) == GlobalRole.SystemAdmin;
        }
        public bool IsManageMaterial(ClaimsPrincipal user)
        {
            var userId = GetCurrentUserId(user);
            var currentUser =  _context.Users.FirstOrDefault(u => u.Id == userId && !u.IsDeleted);
            return currentUser.PositionId == Guid.Parse("6ca448e5-f611-4e72-bb08-b05ff432454a");
        }

        public bool HasPermission(ClaimsPrincipal user, string permissionCode)
        {
            if (IsAdmin(user)) return true;
            if (string.IsNullOrWhiteSpace(permissionCode)) return true;

            var permissions = GetUserPermissions(user);
            return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }

        public bool HasAnyPermission(ClaimsPrincipal user, params string[] permissionCodes)
        {
            // Admin bypass - khong hardcode user id
            if (IsAdmin(user)) return true;
            if (permissionCodes == null || permissionCodes.Length == 0) return true;

            var permissions = GetUserPermissions(user);
            return permissionCodes.Any(code => permissions.Contains(code, StringComparer.OrdinalIgnoreCase));
        }

        public bool HasAllPermissions(ClaimsPrincipal user, params string[] permissionCodes)
        {
            if (IsAdmin(user)) return true;
            if (permissionCodes == null || permissionCodes.Length == 0) return true;

            var permissions = GetUserPermissions(user);
            return permissionCodes.All(code => permissions.Contains(code, StringComparer.OrdinalIgnoreCase));
        }

        public ICollection<string> GetUserPermissions(ClaimsPrincipal user)
        {
            if (user == null || user.Identity?.IsAuthenticated != true)
                return new List<string>();

            return user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
