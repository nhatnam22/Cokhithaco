using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Machine;
using CoKhiThaCo.Models.Dtos.Planning;
using CoKhiThaCo.Models.Enums;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/planning")]
    [Authorize]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService _service;

        public PlanningController(IPlanningService service)
        {
            _service = service;
        }

        [HttpGet]
        [RequirePermission("Planning.View")]
        public IActionResult GetPaged([FromQuery] PlanningListQuery query)
        {
            var result = _service.GetPaged(query, User);
            return Ok(ApiResponse<PagedResult<PlanningDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("Planning.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Kế hoạch không tồn tại"));
            return Ok(ApiResponse<PlanningDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("Planning.Create")]
        public IActionResult Create([FromBody] CreatePlanningRequest request)
        {
            try
            {
                var result = _service.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<PlanningDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/schedule")]
        [RequirePermission("Planning.Create")]
        public IActionResult Schedule(Guid id, [FromBody] SchedulePlanningRequest request)
        {
            try
            {
                var result = _service.Schedule(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Kế hoạch này không tồn tại"));
                return Ok(ApiResponse<PlanningDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("batch-schedule")]
        [RequirePermission("Planning.Schedule")]
        public IActionResult BatchSchedule([FromBody] BatchScheduleRequest request)
        {
            try
            {
                var result = _service.BatchSchedule(request);
                return Ok(ApiResponse<List<PlanningDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("Planning.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdatePlanningRequest request)
        {
            try
            {
                var result = _service.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Kế hoạch này không tồn tại"));
                return Ok(ApiResponse<PlanningDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("batch-schedule")]
        [RequirePermission("Planning.Update")]
        public IActionResult BatchUpdateSchedule([FromBody] BatchUpdateScheduleRequest request)
        {
            try
            {
                var result = _service.BatchUpdateSchedule(request);
                return Ok(ApiResponse<List<PlanningDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("Planning.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _service.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Kế hoạch này không tồn tại"));
                return Ok(ApiResponse.Ok("Deleted"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("summary")]
        [RequirePermission("Planning.View")]
        public IActionResult Summary()
        {
            var result = _service.Summary();
            return Ok(ApiResponse<SummaryStatusPlan>.Ok(result));
        }
        [HttpPatch("status/{id}")]
        [RequirePermission("Planning.Update")]
        public IActionResult UpdateStatus(Guid id, PlanningStatus status)
        {
            try
            {
                _service.UpdateStatus(id, status);
                return Ok(ApiResponse.Ok("Cập nhật trạng thái thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
        [HttpPost("re-allocation")]
        [RequirePermission("Planning.Update")]
        public IActionResult ReAllocation(Guid id)
        {
            try
            {
                _service.ReAllocation(id);
                return Ok(ApiResponse.Ok("Chuyển về phân bổ lại"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("by-machine")]
        [RequirePermission("Planning.View")]
        public IActionResult GetByMachine([FromQuery] MachineListQuery query, [FromQuery] DateTime date)
        {
            var result = _service.GetPlanningGroupMachine(query, date);
            return Ok(ApiResponse<ICollection<PlanningGroupMachine>>.Ok(result));
        }
    }
}