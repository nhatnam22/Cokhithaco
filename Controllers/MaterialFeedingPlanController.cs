using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.MaterialFeedingPlan;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/material-feeding-plans")]
    [Authorize]
    public class MaterialFeedingPlanController : ControllerBase
    {
        private readonly IMaterialFeedingPlanService _service;

        public MaterialFeedingPlanController(IMaterialFeedingPlanService service)
        {
            _service = service;
        }

        [HttpGet]
        [RequirePermission("MaterialFeedingPlan.View")]
        public IActionResult GetPaged([FromQuery] MaterialFeedingPlanListQuery query)
        {
            var result = _service.GetPaged(query);
            return Ok(ApiResponse<PagedResult<MaterialFeedingPlanDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("MaterialFeedingPlan.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Không tìm thấy kế hoạch cấp phôi"));
            return Ok(ApiResponse<MaterialFeedingPlanDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("MaterialFeedingPlan.Create")]
        public IActionResult Create([FromBody] CreateMaterialFeedingPlanRequest request)
        {
            try
            {
                var result = _service.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<MaterialFeedingPlanDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("MaterialFeedingPlan.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateMaterialFeedingPlanRequest request)
        {
            try
            {
                var result = _service.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Không tìm thấy kế hoạch cấp phôi"));
                return Ok(ApiResponse<MaterialFeedingPlanDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/complete")]
        [RequirePermission("MaterialFeedingPlan.ToggleStatus")]
        public IActionResult Complete(Guid id, [FromBody] CompleteMaterialFeedingPlanRequest request)
        {
            try
            {
                var result = _service.Complete(id, request, User);
                return Ok(ApiResponse<MaterialFeedingPlanDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("MaterialFeedingPlan.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _service.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Không tìm thấy kế hoạch cấp phôi"));
                return Ok(ApiResponse.Ok("Deleted"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
        [HttpGet("summary")]
        [RequirePermission("MaterialFeedingPlan.View")]
        public IActionResult Summary()
        {
            var result = _service.Summary();
            if (result == null) return NotFound(ApiResponse.Fail("Tổng quan kế hoạch cấp phôi không tìm thấy"));
            return Ok(ApiResponse<SummaryMaterialFeedingStatus>.Ok(result));
        }

        [HttpGet("summary-today")]
        [RequirePermission("MaterialFeedingPlan.View")]
        public IActionResult SummaryToday()
        {
            var result = _service.SummaryToday();
            return Ok(ApiResponse<SummaryTodayMaterialFeedingStatus>.Ok(result));
        }
    }
}