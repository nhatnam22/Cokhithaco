using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Position;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PositionController : ControllerBase
    {
        private readonly IPositionService _positionService;

        public PositionController(IPositionService positionService)
        {
            _positionService = positionService;
        }

        [HttpGet]
        [RequirePermission("Position.View")]
        public IActionResult GetAll()
        {
            var result = _positionService.GetAll();
            return Ok(ApiResponse<ICollection<PositionDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("Position.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _positionService.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Position not found"));
            return Ok(ApiResponse<PositionDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("Position.Create")]
        public IActionResult Create([FromBody] CreatePositionRequest request)
        {
            try
            {
                var result = _positionService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.PositionId }, ApiResponse<PositionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("Position.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdatePositionRequest request)
        {
            try
            {
                var result = _positionService.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Position not found"));
                return Ok(ApiResponse<PositionDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("Position.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _positionService.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Position not found"));
                return Ok(ApiResponse.Ok("Deleted"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
