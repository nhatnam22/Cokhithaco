using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Export
{
    public class PositionExportDto
    {
        public int RowNo { get; set; }
        public string PositionName { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class PositionExportRequest
    {
        public string Filters { get; set; }
    }
}
