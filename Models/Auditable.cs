using System;
using System.ComponentModel.DataAnnotations.Schema;
using CoKhiThaCo.Data;

namespace CoKhiThaCo.Models
{
    public class Auditable
    {
        public DateTime CreatedDate { get; set; }
        [ForeignKey("User_Created")]
        public Guid? CreatedBy { get; set; }
        public ApplicationUser User_Created { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public Guid? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid? DeletedBy { get; set; }
    }
}
