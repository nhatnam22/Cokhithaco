using System;
using System.Collections.Generic;
using System.Linq;
using CoKhiThaCo.Data;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos.Permission;

namespace CoKhiThaCo.Services
{
    public interface IPermissionService
    {
        ICollection<PermissionDto> GetAll();
        PermissionDto GetById(Guid id);
        PermissionDto Create(CreatePermissionRequest request);
        PermissionDto Update(Guid id, UpdatePermissionRequest request);
        bool Delete(Guid id);
        ICollection<PermissionDto> GetByPositionId(Guid positionId);
        bool AssignToPosition(Guid positionId, List<Guid> permissionIds);
        ICollection<PermissionDto> GetPermissionsOfUser(Guid userId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PermissionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ICollection<PermissionDto> GetAll()
        {
            var repo = _unitOfWork.GetRepository<Permission>();
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menus = menuRepo.GetAll(m => !m.IsDeleted).ToDictionary(m => m.Id, m => m.Name);

            return repo.GetAll(p => !p.IsDeleted, orderBy: q => q.OrderBy(p => p.OrderDisplay))
                .Select(p => MapToDto(p, menus))
                .ToList();
        }

        public PermissionDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Permission>();
            var permission = repo.GetSingle(p => p.Id == id && !p.IsDeleted);
            if (permission == null) return null;

            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menuName = permission.MenuId.HasValue
                ? menuRepo.GetSingle(m => m.Id == permission.MenuId.Value)?.Name
                : null;

            return MapToDto(permission, menuName == null ? null : new Dictionary<Guid, string> { { permission.MenuId.Value, menuName } });
        }

        public PermissionDto Create(CreatePermissionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                throw new Exception("Hãy nhập mã quyền và tên quyền");

            var repo = _unitOfWork.GetRepository<Permission>();
            if (repo.Exists(p => p.Code == request.Code && !p.IsDeleted))
                throw new Exception("Mã quyền đã tồn tại");

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                MenuId = request.MenuId,
                OrderDisplay = request.OrderDisplay,
                CreatedDate = Helpers.TimeHelper.Now
            };

            repo.Add(permission);
            _unitOfWork.Complete();
            return MapToDto(permission, null);
        }

        public PermissionDto Update(Guid id, UpdatePermissionRequest request)
        {
            var repo = _unitOfWork.GetRepository<Permission>();
            var permission = repo.GetById(id);
            if (permission == null || permission.IsDeleted) return null;

            if (repo.Exists(p => p.Code == request.Code && p.Id != id && !p.IsDeleted))
                throw new Exception("Mã quyền đã tồn tại");

            permission.Code = request.Code;
            permission.Name = request.Name;
            permission.Description = request.Description;
            permission.MenuId = request.MenuId;
            permission.OrderDisplay = request.OrderDisplay;
            permission.UpdatedDate = Helpers.TimeHelper.Now;

            repo.Update(permission);
            _unitOfWork.Complete();
            return MapToDto(permission, null);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Permission>();
            var permission = repo.GetById(id);
            if (permission == null || permission.IsDeleted) return false;

            permission.IsDeleted = true;
            permission.DeletedDate = Helpers.TimeHelper.Now;
            repo.Update(permission);
            _unitOfWork.Complete();
            return true;
        }

        public ICollection<PermissionDto> GetByPositionId(Guid positionId)
        {
            var ppRepo = _unitOfWork.GetRepository<PositionsPermission>();
            var permissionRepo = _unitOfWork.GetRepository<Permission>();
            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var menus = menuRepo.GetAll(m => !m.IsDeleted).ToDictionary(m => m.Id, m => m.Name);

            var permissionIds = ppRepo.GetAll(pp => pp.PositionId == positionId && !pp.IsDeleted)
                .Select(pp => pp.PermissionId)
                .ToHashSet();

            return permissionRepo.GetAll(p => permissionIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => MapToDto(p, menus))
                .ToList();
        }

        public bool AssignToPosition(Guid positionId, List<Guid> permissionIds)
        {
            var ppRepo = _unitOfWork.GetRepository<PositionsPermission>();
            var existing = ppRepo.GetAll(pp => pp.PositionId == positionId && !pp.IsDeleted);

            foreach (var old in existing)
            {
                if (permissionIds == null || !permissionIds.Contains(old.PermissionId))
                {
                    old.IsDeleted = true;
                    old.DeletedDate = Helpers.TimeHelper.Now;
                    ppRepo.Update(old);
                }
            }

            var existingIds = existing.Select(e => e.PermissionId).ToHashSet();
            foreach (var permissionId in permissionIds ?? new List<Guid>())
            {
                if (!existingIds.Contains(permissionId))
                {
                    ppRepo.Add(new PositionsPermission
                    {
                        Id = Guid.NewGuid(),
                        PositionId = positionId,
                        PermissionId = permissionId,
                        CreatedDate = Helpers.TimeHelper.Now
                    });
                }
            
            }

            _unitOfWork.Complete();
            return true;
        }

        public ICollection<PermissionDto> GetPermissionsOfUser(Guid userId)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var user = userRepo.GetById(userId);
            if (user?.PositionId == null) return new List<PermissionDto>();
            return GetByPositionId(user.PositionId.Value);
        }

        private static PermissionDto MapToDto(Permission p, Dictionary<Guid, string> menus)
        {
            return new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                MenuId = p.MenuId,
                MenuName = p.MenuId.HasValue && menus != null && menus.TryGetValue(p.MenuId.Value, out var name) ? name : null,
                OrderDisplay = p.OrderDisplay
            };
        }
    }
}
