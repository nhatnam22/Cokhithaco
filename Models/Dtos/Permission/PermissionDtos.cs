using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Permission
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? MenuId { get; set; }
        public string MenuName { get; set; }
        public int OrderDisplay { get; set; }
    }

    public class CreatePermissionRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? MenuId { get; set; }
        public int OrderDisplay { get; set; }
    }

    public class UpdatePermissionRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? MenuId { get; set; }
        public int OrderDisplay { get; set; }
    }

    public class AssignPositionPermissionsRequest
    {
        public Guid PositionId { get; set; }
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}
