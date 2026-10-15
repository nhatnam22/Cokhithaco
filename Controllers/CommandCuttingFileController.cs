using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.CommandCuttingFile;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/command-cutting-files")]
    [Authorize]
    public class CommandCuttingFileController : ControllerBase
    {
        private readonly ICommandCuttingFileService _service;

        public CommandCuttingFileController(ICommandCuttingFileService service)
        {
            _service = service;
        }

        [HttpGet]
        [RequirePermission("CommandCuttingFile.View")]
        public IActionResult GetPaged([FromQuery] CommandCuttingFileListQuery query)
        {
            var result = _service.GetPaged(query);
            return Ok(ApiResponse<PagedResult<CommandCuttingFileDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("CommandCuttingFile.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Command cutting file not found"));
            return Ok(ApiResponse<CommandCuttingFileDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("CommandCuttingFile.Create")]
        public IActionResult Create([FromBody] CreateCommandCuttingFileRequest request)
        {
            try
            {
                var result = _service.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CommandCuttingFileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("CommandCuttingFile.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateCommandCuttingFileRequest request)
        {
            try
            {
                var result = _service.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Command cutting file not found"));
                return Ok(ApiResponse<CommandCuttingFileDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("CommandCuttingFile.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _service.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Command cutting file not found"));
                return Ok(ApiResponse.Ok("Deleted"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("import")]
        [RequirePermission("CommandCuttingFile.Create")]
        public IActionResult Import(IFormFile file)
        {
            try
            {
                var result = _service.ImportFromExcel(file);
                return Ok(ApiResponse<ImportCommandCuttingFileResultDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
