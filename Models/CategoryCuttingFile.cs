using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class CategoryCuttingFile : Auditable
    {
        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsMaterial { get; set; }
    }
}
