using System;
using System.Collections.Generic;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.WorkShift;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkShiftController : ControllerBase
    {
        private readonly IWorkShiftService _workShiftService;

        public WorkShiftController(IWorkShiftService workShiftService)
        {
            _workShiftService = workShiftService;
        }

        [HttpGet]
        [RequirePermission("WorkShift.View")]
        public IActionResult GetPaged([FromQuery] WorkShiftListQuery query)
        {
            var result = _workShiftService.GetPaged(query);
            return Ok(ApiResponse<PagedResult<WorkShiftDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("WorkShift.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _workShiftService.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Ca làm việc không tồn tại"));
            return Ok(ApiResponse<WorkShiftDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("WorkShift.Create")]
        public IActionResult Create([FromBody] CreateWorkShiftRequest request)
        {
            try
            {
                var result = _workShiftService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<WorkShiftDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("WorkShift.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateWorkShiftRequest request)
        {
            try
            {
                var result = _workShiftService.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Ca làm việc không tồn tại"));
                return Ok(ApiResponse<WorkShiftDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("WorkShift.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _workShiftService.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Ca làm việc không tồn tại"));
                return Ok(ApiResponse.Ok("Ca làm việc đã được xóa"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("{workShiftId}/breaks")]
        [RequirePermission("ShiftBreak.View")]
        public IActionResult GetShiftBreaks(Guid workShiftId)
        {
            var result = _workShiftService.GetShiftBreaks(workShiftId);
            return Ok(ApiResponse<List<ShiftBreakDto>>.Ok(result));
        }

        [HttpPost("breaks")]
        [RequirePermission("ShiftBreak.Create")]
        public IActionResult CreateShiftBreak([FromBody] CreateShiftBreakRequest request)
        {
            try
            {
                var result = _workShiftService.CreateShiftBreak(request);
                return Ok(ApiResponse<ShiftBreakDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("breaks/{id}")]
        [RequirePermission("ShiftBreak.Update")]
        public IActionResult UpdateShiftBreak(Guid id, [FromBody] UpdateShiftBreakRequest request)
        {
            try
            {
                var result = _workShiftService.UpdateShiftBreak(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Ca nghỉ không tồn tại"));
                return Ok(ApiResponse<ShiftBreakDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
        [HttpDelete("breaks/{id}")]
        [RequirePermission("ShiftBreak.Delete")]
        public IActionResult DeleteShiftBreak(Guid id)
        {
            try
            {
                var result = _workShiftService.DeleteShiftBreak(id);
                if (!result) return NotFound(ApiResponse.Fail("Ca nghỉ không tồn tại"));
                return Ok(ApiResponse.Ok("Đã xóa ca nghỉ"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}