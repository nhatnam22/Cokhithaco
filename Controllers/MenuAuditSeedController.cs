using CoKhiThaCo.Attributes;
using CoKhiThaCo.Data;
using CoKhiThaCo.Helpers;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Menu;
using CoKhiThaCo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;
        private readonly IPermissionHelper _permissionHelper;

        public MenuController(IMenuService menuService, IPermissionHelper permissionHelper)
        {
            _menuService = menuService;
            _permissionHelper = permissionHelper;
        }

        [HttpGet]
        [RequirePermission("Menu.View")]
        public IActionResult GetAll()
        {
            var result = _menuService.GetAll();
            return Ok(result);
        }

        [HttpGet("my-menus")]
        public IActionResult GetMyMenus()
        {
            var userId = _permissionHelper.GetCurrentUserId(User);
            var result = _menuService.GetMenusByUser(userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("Menu.View")]
        public IActionResult GetById(Guid id)
        {
            var result = _menuService.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("Menu.Create")]
        public IActionResult Create([FromBody] CreateMenuRequest request)
        {
            try
            {
                var result = _menuService.Create(request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { State = "Error", Msg = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("Menu.Update")]
        public IActionResult Update(Guid id, [FromBody] UpdateMenuRequest request)
        {
            try
            {
                var result = _menuService.Update(id, request);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { State = "Error", Msg = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("Menu.Delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                var result = _menuService.Delete(id);
                if (!result) return NotFound();
                return Ok(new { State = "Success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { State = "Error", Msg = ex.Message });
            }
        }
    }

    [ApiController]
    [Route("api/audit-logs")]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [RequirePermission("AuditLog.View")]
        public IActionResult GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string entityName = null, [FromQuery] string action = null)
        {
            var result = _auditLogService.GetPaged(page, pageSize, entityName, action);
            return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
        }
    }

    [ApiController]
    [Route("api/seed")]
    public class SeedController : ControllerBase
    {
        private readonly MyDbContext _context;

        public SeedController(MyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Chi dung khi phat trien: seed du lieu mau. Khong dung production.
        /// </summary>
        [HttpPost]
        public IActionResult Run()
        {
            var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var roleManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ApplicationRole>>();
            SeedData.Initialize(_context, userManager, roleManager);
            return Ok(ApiResponse.Ok("Seed completed"));
        }
    }
}
