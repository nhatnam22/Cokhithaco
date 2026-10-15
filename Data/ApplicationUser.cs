using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CoKhiThaCo.Data
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; }
        public Models.Enums.GlobalRole GlobalRole { get; set; }
        public Guid? PositionId { get; set; }
        public Models.Position Position { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePass { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? ParentUserId { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public Guid? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }

    public class ApplicationRole : IdentityRole<Guid>
    {
        public string Description { get; set; }
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }

    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        public virtual ApplicationUser User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }
}
