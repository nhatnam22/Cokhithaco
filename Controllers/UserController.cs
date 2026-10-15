using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.User;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [RequirePermission("User.View")]
        public IActionResult GetPaged([FromQuery] UserListQuery query)
        {
            var result = _userService.GetPaged(query);
            return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        [RequirePermission("User.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _userService.GetById(id);
            if (result == null) return NotFound(ApiResponse.Fail("Người dùng không tồn tại"));
            return Ok(ApiResponse<UserDto>.Ok(result));
        }

        [HttpPost]
        [RequirePermission("User.Create")]
        public IActionResult Create([FromBody] CreateUserRequest request)
        {
            try
            {
                var result = _userService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<UserDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("User.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var result = _userService.Update(id, request);
                if (result == null) return NotFound(ApiResponse.Fail("Người dùng không tồn tại"));
                return Ok(ApiResponse<UserDto>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("User.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _userService.Delete(id);
                if (!result) return NotFound(ApiResponse.Fail("Người dùng không tồn tại"));
                return Ok(ApiResponse.Ok("Đã xóa người dùng"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/toggle-status")]
        [RequirePermission("User.ToggleStatus")]
        public IActionResult ToggleStatus(Guid id)
        {
            try
            {
                var result = _userService.ToggleStatus(id);
                if (!result) return NotFound(ApiResponse.Fail("Người dùng không tồn tại"));
                return Ok(ApiResponse.Ok("Đã chuyển đổi trạng thái"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
