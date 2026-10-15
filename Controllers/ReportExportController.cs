using System;
using CoKhiThaCo.Attributes;
using CoKhiThaCo.Models.Dtos;
using CoKhiThaCo.Models.Dtos.Export;
using CoKhiThaCo.Services.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoKhiThaCo.Controllers
{
    [ApiController]
    [Route("api/report-export")]
    [Authorize]
    public class ReportExportController : ControllerBase
    {
        private readonly IPositionReportExportService _positionReportExportService;

        public ReportExportController(IPositionReportExportService positionReportExportService)
        {
            _positionReportExportService = positionReportExportService;
        }

        [HttpGet("positions/excel")]
        [RequirePermission("Position.View")]
        public IActionResult ExportPositionsExcel([FromQuery] PositionExportRequest request)
        {
            try
            {
                var stream = _positionReportExportService.ExportToExcel(request);
                var fileName = $"DanhSachChucVu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpGet("positions/pdf")]
        [RequirePermission("Position.View")]
        public IActionResult ExportPositionsPdf([FromQuery] PositionExportRequest request)
        {
            try
            {
                var stream = _positionReportExportService.ExportToPdf(request);
                var fileName = $"DanhSachChucVu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(stream, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
