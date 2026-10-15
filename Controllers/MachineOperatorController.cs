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
    [Route("api/machine-operator")]
    [Authorize]
    public class MachineOperatorController : ControllerBase
    {
        private readonly IMachineOperatorService _service;

        public MachineOperatorController(IMachineOperatorService service)
        {
            _service = service;
        }

        [HttpGet("user/{userId}")]
        [RequirePermission("MachineOperator.View")]
        public IActionResult GetMachinesByUser(Guid userId)
        {
            var result = _service.GetMachinesByUserId(userId);
            return Ok(ApiResponse<ICollection<MachineOperatorDto>>.Ok(result));
        }

        [HttpGet("machine/{machineId}")]
        [RequirePermission("MachineOperator.View")]
        public IActionResult GetUsersByMachine(Guid machineId)
        {
            var result = _service.GetUsersByMachineId(machineId);
            return Ok(ApiResponse<ICollection<MachineOperatorDto>>.Ok(result));
        }

        [HttpGet("my-machines")]
        [Authorize]
        public IActionResult GetMyMachines()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var result = _service.GetMachinesByUserId(Guid.Parse(userIdClaim));
            return Ok(ApiResponse<ICollection<MachineOperatorDto>>.Ok(result));
        }

        [HttpPost("assign-machines")]
        [RequirePermission("MachineOperator.Assign")]
        public IActionResult AssignMachinesToUser([FromBody] AssignMachinesToUserRequest request)
        {
            try
            {
                _service.AssignMachinesToUser(request);
                return Ok(ApiResponse.Ok("Gán máy cho người dùng thành công"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{machineId}/{userId}")]
        [RequirePermission("MachineOperator.Assign")]
        public IActionResult Remove(Guid machineId, Guid userId)
        {
            try
            {
                var result = _service.Remove(machineId, userId);
                if (!result) return NotFound(ApiResponse.Fail("Không tìm thấy bản ghi"));
                return Ok(ApiResponse.Ok("Đã xóa"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}