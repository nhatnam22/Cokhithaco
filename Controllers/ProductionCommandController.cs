using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.ProductionCommand;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/production-commands")]
    [Authorize]
    public class ProductionCommandController : ControllerBase
    {
        private readonly IProductionCommandService _service;

        public ProductionCommandController(IProductionCommandService service)
        {
            _service = service;
        }

        [HttpGet]
        [RequirePermission("ProductionCommand.View")]
        public IActionResult GetPaged([FromQuery] ProductionCommandListQuery query)
        {
            var result = _service.GetPaged(query);
            return Ok(ApiResponse<PagedResult<ProductionCommandDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("ProductionCommand.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Không tìm thấy lệnh sản xuất"));
            return Ok(ApiResponse<ProductionCommandDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("ProductionCommand.Create")]
        public IActionResult Create([FromBody] CreateProductionCommandRequest request)
        {
            try
            {
                var result = _service.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ProductionCommandDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("ProductionCommand.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateProductionCommandRequest request)
        {
            try
            {
                var result = _service.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Không tìm thấy lệnh sản xuất"));
                return Ok(ApiResponse<ProductionCommandDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("ProductionCommand.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _service.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Không tìm thấy lệnh sản xuất"));
                return Ok(ApiResponse.Ok("Deleted"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("import")]
        [RequirePermission("ProductionCommand.Import")]
        public IActionResult Import(IFormFile file)
        {
            try
            {
                var result = _service.ImportFromExcel(file);
                return Ok(ApiResponse<ImportResultDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
