using Shared.EWS.DataModel.Request;
using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Attendance
{
    public class AddAttendanceRequest
    {
        public AttendanceStatus Status { get; set; }
    }

    public class AdminAddAttendanceRequest
    {
        public int UserId { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTime? AttendanceDate { get; set; }
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
}