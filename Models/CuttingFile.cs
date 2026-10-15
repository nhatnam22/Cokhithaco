using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoKhiThaCo.Models
{
    public class CuttingFile : Auditable
    {
        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryCuttingFileId { get; set; }
        [ForeignKey("CategoryCuttingFileId")]
        public CategoryCuttingFile CategoryCuttingFile { get; set; }
    }
}
