using System.ComponentModel.DataAnnotations;

namespace CoKhiThaCo.Models
{
    public class ProductionCommand : Auditable
    {
        [Key]
        public Guid Id { get; set; }
        public string CommandCode { get; set; }
        public string OrderName { get; set; }
    }
}
