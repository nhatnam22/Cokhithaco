//using System;
//using CoKhiThaCo.Attributes;
//using CoKhiThaCo.Models.Dtos;
//using CoKhiThaCo.Models.Dtos.Machine;
//using CoKhiThaCo.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace CoKhiThaCo.Controllers
//{
//    [ApiController]
//    [Route("api/machine-category-cutting-files")]
//    [Authorize]
//    public class MachineCategoryCuttingFileController : ControllerBase
//    {
//        private readonly IMachineCategoryCuttingFileService _service;

//        public MachineCategoryCuttingFileController(IMachineCategoryCuttingFileService service)
//        {
//            _service = service;
//        }

//        [HttpGet("machine/{machineId}")]
//        [RequirePermission("MachineCategoryCuttingFile.View")]
//        public IActionResult GetByMachine(Guid machineId)
//        {
//            var result = _service.GetByMachineId(machineId);
//            return Ok(ApiResponse<ICollection<MachineCategoryCuttingFileDto>>.Ok(result));
//        }

//        [HttpGet("category/{categoryId}")]
//        [RequirePermission("MachineCategoryCuttingFile.View")]
//        public IActionResult GetByCategory(Guid categoryId)
//        {
//            var result = _service.GetByCategoryId(categoryId);
//            return Ok(ApiResponse<ICollection<MachineCategoryCuttingFileDto>>.Ok(result));
//        }

//        [HttpPost("assign")]
//        [RequirePermission("MachineCategoryCuttingFile.Assign")]
//        public IActionResult Assign([FromBody] AssignCategoryCuttingFilesRequest request)
//        {
//            try
//            {
//                _service.Assign(request);
//                return Ok(ApiResponse.Ok("Assigned"));
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ApiResponse.Fail(ex.Message));
//            }
//        }

//        [HttpDelete("{machineId}/{categoryId}")]
//        [RequirePermission("MachineCategoryCuttingFile.Assign")]
//        public IActionResult Remove(Guid machineId, Guid categoryId)
//        {
//            try
//            {
//                var result = _service.Remove(machineId, categoryId);
//                if (!result) return NotFound(ApiResponse.Fail("Gán loại tệp cắt không tồn tại"));
//                return Ok(ApiResponse.Ok("Đã xóa gán loại tệp cắt"));
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ApiResponse.Fail(ex.Message));
//            }
//        }
//    }
//}