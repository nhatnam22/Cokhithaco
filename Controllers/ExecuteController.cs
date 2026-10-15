using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Execute;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/execute")]
    [Authorize]
    public class ExecuteController : ControllerBase
    {
        private readonly IExecuteService _service;

        public ExecuteController(IExecuteService service)
        {
            _service = service;
        }

        [HttpGet]
        //[RequirePermission("Execute.View")]
        public IActionResult GetPaged([FromQuery] ExecuteListQuery query)
        {
            var result = _service.GetPaged(query, User);
            return Ok(ApiResponse<PagedResult<ExecuteDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        //[RequirePermission("Execute.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Thực hiện gia công không tìm thấy"));
            return Ok(ApiResponse<ExecuteDto>.Ok(result));
        }

        //[HttpPost("start")]
        //[RequirePermission("Execute.Start")]
        //public IActionResult Start([FromBody] StartExecuteRequest request)
        //{
        //    try
        //    {
        //        var result = _service.Start(request, User);
        //        return Ok(ApiResponse<ExecuteDto>.Ok(result));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}

        //[HttpPost("{id}/complete")]
        //[RequirePermission("Execute.Complete")]
        //public IActionResult Complete(Guid id, [FromBody] CompleteExecuteRequest request)
        //{
        //    try
        //    {
        //        var result = _service.Complete(id, request);
        //        return Ok(ApiResponse<ExecuteDto>.Ok(result));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}

        //[HttpPost("{id}/cancel")]
        //[RequirePermission("Execute.Complete")]
        //public IActionResult Cancel(Guid id, [FromBody] CancelExecuteRequest request)
        //{
        //    try
        //    {
        //        var result = _service.Cancel(id, request);
        //        return Ok(ApiResponse<ExecuteDto>.Ok(result));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse.Fail(ex.Message));
        //    }
        //}

        [HttpGet("summary")]
        [RequirePermission("Execute.View")]
        public IActionResult Summary()
        {
            var result = _service.Summary();
            if (result == null) return NotFound(ApiResponse.Fail("Tổng quan thực thi không tìm thấy"));
            return Ok(ApiResponse<SummaryStatusExecute>.Ok(result));
        }

        [HttpPost("{id}/receive")]
        [RequirePermission("MaterialReceipt.Create")]
        public IActionResult ReceiveMaterialFeed(Guid id, [FromBody] ComfirmReceiveRequest request)
        {
            try
            {
                var result = _service.ReceiveMaterialFeed(id, request);
                return Ok(ApiResponse<ExecuteDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("sub-execute/start")]
        [RequirePermission("Execute.ToggleStatus")]
        public IActionResult StartSubExecute([FromBody] StartSubExecuteRequest request)
        {
            try
            {
                var result = _service.StartSubExecute(request, User);
                return Ok(ApiResponse<SubExecuteDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("sub-execute/end")]
        [RequirePermission("Execute.ToggleStatus")]
        public IActionResult EndSubExecute([FromBody] EndSubExecuteRequest request)
        {
            try
            {
                var subExecuteId = request.ExecuteId;
                var result = _service.EndSubExecute(subExecuteId, request, User);
                return Ok(ApiResponse<SubExecuteDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
        [HttpPost("sub-execute/continute")]
        [RequirePermission("Execute.ToggleStatus")]
        public IActionResult ContinuteSubExecute([FromBody] ContitnuteSubExecuteRequest request)
        {
            try
            {
                var subExecuteId = request.ExecuteId;
                var result = _service.ContinuteSubExecute(subExecuteId, User);
                return Ok(ApiResponse<SubExecuteDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
        [HttpPost("complete")]
        [RequirePermission("Execute.Complete")]
        public IActionResult CompletedExecute([FromBody] CompletedExecute request)
        {
            try
            {
                var executeId = request.ExecuteId;
                _service.Completed(executeId);
                return Ok(ApiResponse.Ok("Đã hoàn thành lệnh thực thi này"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}