using System;
using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string EntityName { get; set; }

        [Required, MaxLength(100)]
        public string EntityId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; }

        [MaxLength(200)]
        public string PropertyName { get; set; }

        [MaxLength(1000)]
        public string OldValue { get; set; }

        [MaxLength(1000)]
        public string NewValue { get; set; }

        public Guid? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
