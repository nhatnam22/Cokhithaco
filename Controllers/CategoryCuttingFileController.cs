using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CategoryCuttingFile;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/category-cutting-files")]
    [Authorize]
    public class CategoryCuttingFileController : ControllerBase
    {
        private readonly ICategoryCuttingFileService _service;

        public CategoryCuttingFileController(ICategoryCuttingFileService service)
        {
            _service = service;
        }

        [HttpGet]
        [RequirePermission("CategoryCuttingFile.View")]
        public IActionResult GetPaged([FromQuery] CategoryCuttingFileListQuery query, [FromQuery] bool isMaterial)
        {
            var result = _service.GetPaged(query, isMaterial);
            return Ok(ApiResponse<PagedResult<CategoryCuttingFileDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("CategoryCuttingFile.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Loại tệp cắt không tồn tại"));
            return Ok(ApiResponse<CategoryCuttingFileDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("CategoryCuttingFile.Create")]
        public IActionResult Create([FromBody] CreateCategoryCuttingFileRequest request)
        {
            try
            {
                var result = _service.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CategoryCuttingFileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("CategoryCuttingFile.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateCategoryCuttingFileRequest request)
        {
            try
            {
                var result = _service.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Loại tệp cắt không tồn tại"));
                return Ok(ApiResponse<CategoryCuttingFileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("CategoryCuttingFile.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _service.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Loại tệp cắt không tồn tại"));
                return Ok(ApiResponse.Ok("Đã xóa loại tệp cắt"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
