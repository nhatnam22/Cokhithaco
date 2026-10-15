using System;
using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class PositionsPermission : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PositionId { get; set; }
        public Position Position { get; set; }

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}
