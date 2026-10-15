using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Execute;
using CoKhiThaCo.Models.Dtos.SummaryPlan;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/summary")]
    [Authorize]
    public class SummaryPlanController : ControllerBase
    {
        private readonly ISummaryPlanService _service;
        public SummaryPlanController(ISummaryPlanService service)
        {
            _service = service;
        }
        [HttpGet]
        [RequirePermission("Summary.View")]
        public IActionResult GetPaged([FromQuery] SummaryRequest query)
        {
            var result = _service.GetSummary(query);
            return Ok(ApiResponse<PagedResult<SummaryPlanDto>>.Ok(result));
        }
    }
}
