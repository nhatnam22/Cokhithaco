using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class Permission : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Code { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public Guid? MenuId { get; set; }

        [ForeignKey("MenuId")]
        public Menu Menu { get; set; }

        public int OrderDisplay { get; set; }
    }
}
