using System;
using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class Menu : Auditable
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Path { get; set; }

        [MaxLength(100)]
        public string Icon { get; set; }

        public Guid? ParentId { get; set; }
        public Menu Parent { get; set; }

        public int OrderDisplay { get; set; }
        public virtual ICollection<Permission> Permissions { get; set; }
    }
}
