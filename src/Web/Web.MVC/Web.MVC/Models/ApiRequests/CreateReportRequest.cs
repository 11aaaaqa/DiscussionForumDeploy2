﻿namespace Web.MVC.Models.ApiRequests
{
    public class CreateReportRequest
    {
        public string UserNameReportedBy { get; set; }
        public Guid UserIdReportedTo { get; set; }
        public string? ReportedCommentContent { get; set; }
        public string? ReportedDiscussionTitle { get; set; }
        public string? ReportedDiscussionContent { get; set; }
        public string Reason { get; set; }
        public string ReportType { get; set; }
        public Guid? ReportedCommentId { get; set; }
        public Guid? ReportedDiscussionId { get; set; }
    }
}
