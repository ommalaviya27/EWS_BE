using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Attendance
{
    public class AddAttendanceRequest
    {
        public AttendanceStatus Status { get; set; }
    }

    public class EditAttendanceRequest
    {
        public AttendanceStatus Status { get; set; }
    }

    public class ReviewAttendanceRequest
    {
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? ReviewerRemark { get; set; }
    }

    public class AttendanceMonthRequest
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int? UserId { get; set; }
    }

    public class AttendanceSearchRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? UserId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public AttendanceStatus? Status { get; set; }
        public ApprovalStatus? ApprovalStatus { get; set; }
    }
}