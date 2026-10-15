using System;
using System.Collections.Generic;

namespace CoKhiThaCo.Models.Dtos.Position
{
    public class PositionDto
    {
        public Guid PositionId { get; set; }
        public string PositionName { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
        public List<string> PermissionCodes { get; set; }
    }

    public class CreatePositionRequest
    {
        public string PositionName { get; set; }
        public string Description { get; set; }
    }

    public class UpdatePositionRequest
    {
        public string PositionName { get; set; }
        public string Description { get; set; }
    }
}
