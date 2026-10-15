using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos.Auth;
using CoKhiThaCo.Models.Dtos.Permission;
using CoKhiThaCo.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoKhiThaCo.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest request);
        Task Logout(Guid userId);
        Task<bool> ChangePassword(Guid userId, ChangePasswordRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionService _permissionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppSettings _appSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IPermissionService permissionService,
            IUnitOfWork unitOfWork,
            IOptions<AppSettings> appSettings)
        {
            _userManager = userManager;
            _permissionService = permissionService;
            _unitOfWork = unitOfWork;
            _appSettings = appSettings.Value;
        }

        public async Task<LoginResponse> Login(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null || user.IsDeleted || !user.IsActive)
                throw new Exception("Tên người dùng không tồn tại hoặc đã bị khóa");

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
                throw new Exception("Tên người dùng hoặc mật khẩu không đúng");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.GlobalRole.ToString())
            };

            var permissions = new List<PermissionDto>();
            if (user.PositionId.HasValue)
            {
                permissions = _permissionService.GetByPositionId(user.PositionId.Value).ToList();
                claims.AddRange(permissions.Select(p => new Claim("permission", p.Code)));
            }

            // Lay danh sach may duoc gan cho user
            var machineIds = _unitOfWork.GetRepository<MachineOperator>()
                .GetAll(mo => mo.UserId == user.Id && !mo.IsDeleted)
                .Select(mo => mo.MachineId.ToString())
                .ToList();
            claims.AddRange(machineIds.Select(id => new Claim("machine", id)));

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = TimeHelper.Now.AddHours(24);

            var token = new JwtSecurityToken(
                issuer: _appSettings.Issuer,
                audience: _appSettings.Issuer,
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                GlobalRole = (int)user.GlobalRole,
                Permissions = permissions?.Select(p => p.Code).ToList() ?? new List<string>(),
                MachineIds = machineIds
            };
        }

        public Task Logout(Guid userId)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> ChangePassword(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (result.Succeeded && user.MustChangePass)
            {
                user.MustChangePass = false;
                await _userManager.UpdateAsync(user);
            }
            return result.Succeeded;
        }
    }
}
