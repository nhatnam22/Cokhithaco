using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Machine;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MachineController : ControllerBase
    {
        private readonly IMachineService _machineService;

        public MachineController(IMachineService machineService)
        {
            _machineService = machineService;
        }

        [HttpGet]
        [RequirePermission("Machine.View")]
        public IActionResult GetPaged([FromQuery] MachineListQuery query)
        {
            var result = _machineService.GetPaged(query);
            return Ok(ApiResponse<PagedResult<MachineDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("Machine.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _machineService.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Máy không tồn tại"));
            return Ok(ApiResponse<MachineDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("Machine.Create")]
        public IActionResult Create([FromBody] CreateMachineRequest request)
        {
            try
            {
                var result = _machineService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.MachineId }, ApiResponse<MachineDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("Machine.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateMachineRequest request)
        {
            try
            {
                var result = _machineService.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Máy không tồn tại"));
                return Ok(ApiResponse<MachineDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("Machine.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _machineService.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Máy không tồn tại"));
                return Ok(ApiResponse.Ok("Đã xóa máy"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        //[HttpPost("{id}/workshifts")]
        //[RequirePermission("Machine.Update")]
        //public IActionResult AssignWorkShifts(Guid id, [FromBody] AssignWorkShiftsToMachineRequest request)
        //{
        //    try
        //    {
        //        request.MachineId = id;
        //        _machineService.AssignWorkShifts(request);
        //        return Ok(ApiResponse.Ok("Gán ca làm việc thành công"));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}

        //[HttpDelete("{id}/workshifts/{workShiftId}")]
        //[RequirePermission("Machine.Update")]
        //public IActionResult RemoveWorkShift(Guid id, Guid workShiftId)
        //{
        //    try
        //    {
        //        _machineService.RemoveWorkShift(id, workShiftId);
        //        return Ok(ApiResponse.Ok("Xóa ca làm việc khỏi máy thành công"));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}

        //[HttpPost("{id}/categories")]
        //[RequirePermission("Machine.Update")]
        //public IActionResult AssignCategories(Guid id, [FromBody] AssignCategoriesToMachineRequest request)
        //{
        //    try
        //    {
        //        request.MachineId = id;
        //        _machineService.AssignCategoryCuttingFiles(request);
        //        return Ok(ApiResponse.Ok("Gán loại file cắt thành công"));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}

        //[HttpDelete("{id}/categories/{categoryId}")]
        //[RequirePermission("Machine.Update")]
        //public IActionResult RemoveCategory(Guid id, Guid categoryId)
        //{
        //    try
        //    {
        //        _machineService.RemoveCategoryCuttingFile(id, categoryId);
        //        return Ok(ApiResponse.Ok("Xóa loại file cắt khỏi máy thành công"));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}
    }
}