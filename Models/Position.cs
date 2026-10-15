using System;
using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class Position : Auditable
    {
        [Key]
        public Guid PositionId { get; set; }

        [Required, MaxLength(200)]
        public string PositionName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
    }
}
