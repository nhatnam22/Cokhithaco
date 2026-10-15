using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Permission;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        [RequirePermission("Permission.View")]
        public IActionResult GetAll()
        {
            var result = _permissionService.GetAll();
            return Ok(ApiResponse<ICollection<PermissionDto>>.Ok(result));
        }

        [HttpGet("position/{positionId}")]
        [RequirePermission("Permission.View")]
        public IActionResult GetByPosition(Guid positionId)
        {
            var result = _permissionService.GetByPositionId(positionId);
            return Ok(ApiResponse<ICollection<PermissionDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("Permission.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _permissionService.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Permission not found"));
            return Ok(ApiResponse<PermissionDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("Permission.Create")]
        public IActionResult Create([FromBody] CreatePermissionRequest request)
        {
            try
            {
                var result = _permissionService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<PermissionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("Permission.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdatePermissionRequest request)
        {
            try
            {
                var result = _permissionService.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Permission not found"));
                return Ok(ApiResponse<PermissionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("Permission.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _permissionService.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Permission not found"));
                return Ok(ApiResponse.Ok("Deleted"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("assign-position")]
        [RequirePermission("Position.AssignPermission")]
        public IActionResult AssignToPosition([FromBody] AssignPositionPermissionsRequest request)
        {
            try
            {
                var result = _permissionService.AssignToPosition(request.PositionId, request.PermissionIds);
                return Ok(ApiResponse.Ok("Assigned"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
