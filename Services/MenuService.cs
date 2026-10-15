using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Infrastructure;
using CoKhiThaCo.Models;
using CoKhiThaCo.Models.Dtos.Menu;
using CoKhiThaCo.Models.Dtos.Permission;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoKhiThaCo.Services
{
    public interface IMenuService
    {
        ICollection<MenuDto> GetAll();
        MenuDto GetById(Guid id);
        MenuDto Create(CreateMenuRequest request);
        MenuDto Update(Guid id, UpdateMenuRequest request);
        bool Delete(Guid id);
        ICollection<MenuDto> GetMenusByUser(Guid userId);
    }

    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MenuService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ICollection<MenuDto> GetAll()
        {
            var repo = _unitOfWork.GetRepository<Menu>();
            var menus = repo.GetAll(
                whereCondition: m => !m.IsDeleted,
                orderBy: q => q.OrderBy(m => m.OrderDisplay).ThenBy(m => m.Name),
                includes: new[] { "Permissions" }
            );

            return BuildTree(menus);
        }

        public MenuDto GetById(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Menu>();
            var menu = repo.GetSingle(
                whereCondition: m => m.Id == id && !m.IsDeleted,
                includes: new[] { "Permissions" }
            );

            if (menu == null)
                return null;

            return MapToDto(menu);
        }

        public MenuDto Create(CreateMenuRequest request)
        {
            var repo = _unitOfWork.GetRepository<Menu>();
            if (repo.Exists(m => m.Name == request.Name && !m.IsDeleted))
                throw new Exception("Tên menu đã tồn tại");

            if (request.ParentId.HasValue)
            {
                var parent = repo.GetById(request.ParentId.Value);
                if (parent == null || parent.IsDeleted)
                    throw new Exception("Tên menu cha không tồn tại");
            }

            var menu = new Menu
            {
                Name = request.Name,
                ParentId = request.ParentId,
                Path = request.Path,
                Icon = request.Icon,
                OrderDisplay = request.OrderDisplay,
                CreatedDate = TimeHelper.Now
            };

            repo.Add(menu);
            _unitOfWork.Complete();

            return GetById(menu.Id);
        }

        public MenuDto Update(Guid id, UpdateMenuRequest request)
        {
            var repo = _unitOfWork.GetRepository<Menu>();
            var menu = repo.GetById(id);
            if (menu == null || menu.IsDeleted) return null;

            if (repo.Exists(m => m.Name == request.Name && m.Id != id && !m.IsDeleted))
                throw new Exception("Tên menu đã tồn tại");

            if (request.ParentId.HasValue && request.ParentId.Value == id)
                throw new Exception("Menu này không thể là cha của chính nó");

            if (request.ParentId.HasValue)
            {
                var parent = repo.GetById(request.ParentId.Value);
                if (parent == null || parent.IsDeleted)
                    throw new Exception("Tên menu cha không tồn tại");
            }

            menu.Name = request.Name;
            menu.ParentId = request.ParentId;
            menu.Path = request.Path;
            menu.Icon = request.Icon;
            menu.OrderDisplay = request.OrderDisplay;
            menu.UpdatedDate = TimeHelper.Now;

            repo.Update(menu);
            _unitOfWork.Complete();

            return GetById(menu.Id);
        }

        public bool Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Menu>();
            var menu = repo.GetById(id);
            if (menu == null || menu.IsDeleted) return false;

            if (repo.Exists(m => m.ParentId == id && !m.IsDeleted))
                throw new Exception("Không thể xóa menu có chứa menu con");

            var permissionRepo = _unitOfWork.GetRepository<Permission>();
            if (permissionRepo.Exists(p => p.MenuId == id && !p.IsDeleted))
                throw new Exception("Không thể xóa menu có chứa quyền hạn");

            menu.IsDeleted = true;
            menu.DeletedDate = TimeHelper.Now;
            repo.Update(menu);
            _unitOfWork.Complete();
            return true;
        }

        public ICollection<MenuDto> GetMenusByUser(Guid userId)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser>();
            var user = userRepo.GetSingle(
                whereCondition: u => u.Id == userId && !u.IsDeleted,
                includes: new[] { "Position" }
            );

            if (user?.PositionId == null)
                return new List<MenuDto>();

            var ppRepo = _unitOfWork.GetRepository<PositionsPermission>();
            var permissionIds = ppRepo.GetAll(
                whereCondition: pp => pp.PositionId == user.PositionId.Value && !pp.IsDeleted,
                includes: new[] { "Permission" }
            )
            .Where(pp => pp.Permission != null && !pp.Permission.IsDeleted && pp.Permission.MenuId.HasValue)
            .Select(pp => pp.PermissionId)
            .Distinct()
            .ToList();

            if (!permissionIds.Any())
                return new List<MenuDto>();

            var permissionRepo = _unitOfWork.GetRepository<Permission>();
            var permissions = permissionRepo.GetAll(
                whereCondition: p => permissionIds.Contains(p.Id) && !p.IsDeleted && p.MenuId.HasValue,
                includes: new[] { "Menu" }
            );

            var menuIds = permissions.Select(p => p.MenuId.Value).Distinct().ToList();

            var menuRepo = _unitOfWork.GetRepository<Menu>();
            var allMenus = menuRepo.GetAll(
                whereCondition: m => !m.IsDeleted,
                orderBy: q => q.OrderBy(m => m.OrderDisplay).ThenBy(m => m.Name)
            );

            // Include parent menus to build complete tree
            var accessibleMenuIds = new HashSet<Guid>(menuIds);
            foreach (var menuId in menuIds)
            {
                var current = allMenus.FirstOrDefault(m => m.Id == menuId);
                while (current?.ParentId.HasValue == true)
                {
                    accessibleMenuIds.Add(current.ParentId.Value);
                    current = allMenus.FirstOrDefault(m => m.Id == current.ParentId.Value);
                }
            }

            var accessibleMenus = allMenus.Where(m => accessibleMenuIds.Contains(m.Id)).ToList();
            var menuDtoLookup = accessibleMenus.ToDictionary(
                m => m.Id,
                m => MapToDto(m, permissions.Where(p => p.MenuId == m.Id).ToList())
            );

            var rootMenus = new List<MenuDto>();
            foreach (var menu in accessibleMenus.OrderBy(m => m.OrderDisplay).ThenBy(m => m.Name))
            {
                var dto = menuDtoLookup[menu.Id];
                if (menu.ParentId.HasValue && menuDtoLookup.ContainsKey(menu.ParentId.Value))
                {
                    var parent = menuDtoLookup[menu.ParentId.Value];
                    if (parent.Children == null)
                        parent.Children = new List<MenuDto>();
                    parent.Children.Add(dto);
                }
                else
                {
                    rootMenus.Add(dto);
                }
            }

            return rootMenus;
        }

        private List<MenuDto> BuildTree(ICollection<Menu> menus)
        {
            var menuLookup = menus.ToDictionary(m => m.Id, m => MapToDto(m));
            var rootMenus = new List<MenuDto>();

            foreach (var menu in menus.OrderBy(m => m.OrderDisplay).ThenBy(m => m.Name))
            {
                var dto = menuLookup[menu.Id];
                if (menu.ParentId.HasValue && menuLookup.ContainsKey(menu.ParentId.Value))
                {
                    var parent = menuLookup[menu.ParentId.Value];
                    if (parent.Children == null)
                        parent.Children = new List<MenuDto>();
                    parent.Children.Add(dto);
                }
                else
                {
                    rootMenus.Add(dto);
                }
            }

            return rootMenus;
        }

        private MenuDto MapToDto(Menu menu)
        {
            return new MenuDto
            {
                Id = menu.Id,
                Name = menu.Name,
                ParentId = menu.ParentId,
                ParentName = menu.Parent?.Name,
                Path = menu.Path,
                Icon = menu.Icon,
                OrderDisplay = menu.OrderDisplay,
                Children = new List<MenuDto>(),
                Permissions = menu.Permissions?
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.OrderDisplay)
                    .ThenBy(p => p.Code)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        MenuId = p.MenuId,
                        MenuName = menu.Name,
                        OrderDisplay = p.OrderDisplay
                    }).ToList() ?? new List<PermissionDto>()
            };
        }

        private MenuDto MapToDto(Menu menu, ICollection<Permission> permissions)
        {
            var dto = MapToDto(menu);
            if (permissions != null && permissions.Any())
            {
                dto.Permissions = permissions
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.OrderDisplay)
                    .ThenBy(p => p.Code)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        MenuId = p.MenuId,
                        MenuName = menu.Name,
                        OrderDisplay = p.OrderDisplay
                    }).ToList();
            }
            return dto;
        }
    }
}
