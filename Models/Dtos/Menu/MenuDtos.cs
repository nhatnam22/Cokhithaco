using CoKhiThaCo.Models.Dtos.Permission;
using System;

namespace CoKhiThaCo.Models.Dtos.Menu
{
    public class MenuDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public string ParentName { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public int OrderDisplay { get; set; }
        public List<MenuDto> Children { get; set; }
        public List<PermissionDto> Permissions { get; set; }
    }

    public class CreateMenuRequest
    {
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public int OrderDisplay { get; set; }
    }

    public class UpdateMenuRequest
    {
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public int OrderDisplay { get; set; }
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string Action { get; set; }
        public string PropertyName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public Guid? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
