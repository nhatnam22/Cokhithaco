using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CuttingFile;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/cutting-files")]
    [Authorize]
    public class CuttingFileController : ControllerBase
    {
        private readonly ICuttingFileService _service;

        public CuttingFileController(ICuttingFileService service)
        {
            _service = service;
        }

        [HttpGet]
        [RequirePermission("CuttingFile.View")]
        public IActionResult GetPaged([FromQuery] CuttingFileListQuery query)
        {
            var result = _service.GetPaged(query);
            return Ok(ApiResponse<PagedResult<CuttingFileDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("CuttingFile.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("File cắt không tồn tại"));
            return Ok(ApiResponse<CuttingFileDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("CuttingFile.Create")]
        public IActionResult Create([FromBody] CreateCuttingFileRequest request)
        {
            try
            {
                var result = _service.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CuttingFileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("CuttingFile.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateCuttingFileRequest request)
        {
            try
            {
                var result = _service.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("File cắt không tồn tại"));
                return Ok(ApiResponse<CuttingFileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("CuttingFile.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _service.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("File cắt không tồn tại"));
                return Ok(ApiResponse.Ok("Đã xóa file cắt"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
