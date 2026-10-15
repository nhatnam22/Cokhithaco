using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Permission;
using CoKhiThaCo.Models.Dtos.User;
using CoKhiThaCo.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace CoKhiThaCo.Services
{
    public interface IUserService
    {
        PagedResult<UserDto> GetPaged(UserListQuery query);
        UserDto GetById(Guid id);
        UserDto Create(CreateUserRequest request);
        UserDto Update(Guid id, UpdateUserRequest request);
        bool Delete(Guid id);
        bool ToggleStatus(Guid id);
    }

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionService _permissionService;

        public UserService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _permissionService = permissionService;
        }

        public PagedResult<UserDto> GetPaged(UserListQuery query)
        {
            var repo = _unitOfWork.GetRepository<ApplicationUser>();
            var positionRepo = _unitOfWork.GetRepository<Position>();

            var sorts = SortHelper.ParseSorts(query.Sorts);
            var filters = FilterHelper.ParseFilters(query.Filters);
            if (sorts.Count == 0)
                sorts.Add(new Infrastructure.SortField { Field = "UserName", Direction = "asc" });

            var items = repo.GetAllPaging(u => !u.IsDeleted 
            && (string.IsNullOrEmpty(query.Keyword) || u.UserName.Contains(query.Keyword) || u.Email.Contains(query.Keyword) || u.FullName.Contains(query.Keyword)), out var total, query.Page, query.PageSize, sorts, filters);
            var positions = positionRepo.GetAll(p => !p.IsDeleted).ToDictionary(p => p.PositionId, p => p.PositionName);

            var dtos = items.Select(u => MapToDto(u, positions)).ToList();

            foreach (var dto in dtos)
            {
                dto.PermissionCodes = dto.PositionId.HasValue
                    ? _permissionService.GetByPositionId(dto.PositionId.Value).Select(p => p.Code).ToList()
                    : new List<string>();
            }

            return new PagedResult<UserDto>
            {
                Items = dtos,
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public UserDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<ApplicationUser>();
            var user = repo.GetSingle(u => u.Id == id && !u.IsDeleted);
            if (user == null) return null;

            var positionRepo = _unitOfWork.GetRepository<Position>();
            var positions = positionRepo.GetAll(p => !p.IsDeleted).ToDictionary(p => p.PositionId, p => p.PositionName);

            var dto = MapToDto(user, positions);
            dto.PermissionCodes = user.PositionId.HasValue
                ? _permissionService.GetByPositionId(user.PositionId.Value).Select(p => p.Code).ToList()
                : new List<string>();
            return dto;
        }

        public UserDto Create(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                throw new Exception("Hãy nhập tên đăng nhập và mật khẩu");
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Hãy nhập email");

            var repo = _unitOfWork.GetRepository<ApplicationUser>();
            if (repo.Exists(u => u.UserName == request.UserName && !u.IsDeleted))
                throw new Exception("Tên đăng nhập đã tồn tại");
            if (repo.Exists(u => u.Email == request.Email && !u.IsDeleted))
                throw new Exception("Email đã tồn tại");

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                GlobalRole = (GlobalRole)request.GlobalRole,
                PositionId = request.PositionId,
                IsActive = true,
                MustChangePass = true,
                EmailConfirmed = true,
                CreatedDate = TimeHelper.Now
            };

            var result = _userManager.CreateAsync(user, request.Password).Result;
            if (!result.Succeeded)
                throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));

            _unitOfWork.Complete();
            return GetById(user.Id);
        }

        public UserDto Update(Guid id, UpdateUserRequest request)
        {
            var user = _userManager.FindByIdAsync(id.ToString()).Result;
            if (user == null || user.IsDeleted) return null;

            var repo = _unitOfWork.GetRepository<ApplicationUser>();
            if (repo.Exists(u => u.Email == request.Email && u.Id != id && !u.IsDeleted))
                throw new Exception("Email đã tồn tại");

            user.FullName = request.FullName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.GlobalRole = (GlobalRole)request.GlobalRole;
            user.PositionId = request.PositionId;
            user.IsActive = request.IsActive;
            user.UpdatedDate = TimeHelper.Now;

            var result = _userManager.UpdateAsync(user).Result;
            if (!result.Succeeded)
                throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));

            _unitOfWork.Complete();
            return GetById(id);
        }

        public bool Delete(Guid id)
        {
            var user = _userManager.FindByIdAsync(id.ToString()).Result;
            if (user == null || user.IsDeleted) return false;

            user.IsDeleted = true;
            user.IsActive = false;
            user.DeletedDate = TimeHelper.Now;
            var result = _userManager.UpdateAsync(user).Result;
            if (!result.Succeeded)
                throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));

            _unitOfWork.Complete();
            return true;
        }

        public bool ToggleStatus(Guid id)
        {
            var user = _userManager.FindByIdAsync(id.ToString()).Result;
            if (user == null || user.IsDeleted) return false;

            user.IsActive = !user.IsActive;
            user.UpdatedDate = TimeHelper.Now;
            var result = _userManager.UpdateAsync(user).Result;
            if (!result.Succeeded)
                throw new Exception(string.Join("; ", result.Errors.Select(e => e.Description)));

            _unitOfWork.Complete();
            return true;
        }

        private static UserDto MapToDto(ApplicationUser u, Dictionary<Guid, string> positions)
        {
            return new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                GlobalRole = (int)u.GlobalRole,
                GlobalRoleName = u.GlobalRole.ToString(),
                PositionId = u.PositionId,
                PositionName = u.PositionId.HasValue && positions.TryGetValue(u.PositionId.Value, out var pn) ? pn : null,
                IsActive = u.IsActive,
                MustChangePass = u.MustChangePass,
                CreatedDate = u.CreatedDate
            };
        }
    }
}
