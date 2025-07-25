﻿using Microsoft.AspNetCore.Mvc;
using ReportMicroservice.Api.DTOs;
using ReportMicroservice.Api.Models;
using ReportMicroservice.Api.Services;

namespace ReportMicroservice.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService<Report> reportService;
        private readonly IPaginationService paginationService;

        public ReportController(IReportService<Report> reportService, IPaginationService paginationService)
        {
            this.reportService = reportService;
            this.paginationService = paginationService;
        }

        [Route("GetAllReports")]
        [HttpGet]
        public async Task<IActionResult> GetAllReportsAsync([FromQuery] ReportParameters reportParameters) => 
            Ok(await reportService.GetAllReportsAsync(reportParameters));

        [Route("DoesNextPageExist")]
        [HttpGet]
        public async Task<IActionResult> DoesNextPageExistAsync([FromQuery] ReportParameters reportParameters)
        {
            var doesExist =
                await paginationService.DoesNextReportsPageExistAsync(reportParameters.PageSize,
                    reportParameters.PageNumber);
            return Ok(doesExist);
        }

        [Route("DoesNextPageByUserNameExist")]
        [HttpGet]
        public async Task<IActionResult> DoesNextPageByUserNameExistAsync([FromQuery] ReportParameters reportParameters, string userName)
        {
            var doesExist =
                await paginationService.DoesNextReportsByUserNamePageExistAsync(reportParameters.PageSize,
                    reportParameters.PageNumber, userName);
            return Ok(doesExist);
        }

        [Route("DoesNextPageByReportTypeExist")]
        [HttpGet]
        public async Task<IActionResult> DoesNextPageByReportTypeExistAsync([FromQuery] ReportParameters reportParameters, string reportType)
        {
            var doesExist =
                await paginationService.DoesNextReportsByReportTypePageExistAsync(reportParameters.PageSize,
                    reportParameters.PageNumber, reportType);
            return Ok(doesExist);
        }

        [Route("GetReportById/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetReportByIdAsync(Guid id)
        {
            var report = await reportService.GetReportByIdAsync(id);
            if (report is null)
                return BadRequest();
            
            return Ok(report);
        }

        [Route("CreateReport")]
        [HttpPost]
        public async Task<IActionResult> CreateReportAsync([FromBody]CreateReportDto model)
        {
            var report = await reportService.CreateReportAsync(new Report
            {
                Id = Guid.NewGuid(),
                Reason = model.Reason,
                ReportedCommentContent = model.ReportedCommentContent,
                ReportedDiscussionContent = model.ReportedDiscussionContent,
                ReportedDiscussionTitle = model.ReportedDiscussionTitle,
                UserIdReportedTo = model.UserIdReportedTo,
                UserNameReportedBy = model.UserNameReportedBy,
                ReportType = model.ReportType,
                ReportedCommentId = model.ReportedCommentId,
                ReportedDiscussionId = model.ReportedDiscussionId
            });
            return Ok(report);
        }

        [Route("DeleteReportsByUserName/{userName}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteReportsByUserNameAsync(string userName)
        {
            await reportService.DeleteReportsByUserName(userName);
            return Ok();
        }

        [Route("GetReportsByReportType/{reportType}")]
        [HttpGet]
        public async Task<IActionResult> GetReportsByReportTypeAsync(string reportType, [FromQuery] ReportParameters reportParameters)
        {
            var reports = await reportService.GetReportsByReportType(reportType, reportParameters);
            if (reports is null)
                return BadRequest();
            return Ok(reports);
        }

        [Route("DeleteReportById/{reportId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteReportByIdAsync(Guid reportId)
        {
            await reportService.DeleteReportById(reportId);
            return Ok();
        }

        [Route("GetReportsByUserName/{userName}")]
        [HttpGet]
        public async Task<IActionResult> GetReportsByUserNameAsync(string userName, [FromQuery] ReportParameters reportParameters) =>
            Ok(await reportService.GetByUserNameAsync(userName, reportParameters));
    }
}
