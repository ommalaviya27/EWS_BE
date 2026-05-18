namespace Domain.EWS.DataModels.Response.User
{
    public class GetUserResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool Status { get; set; }
        public int? TeamLeadId { get; set; }
        public string? TeamLeadName { get; set; }
    }
    
    public class UserSummary
    {
        public int TotalEmployees { get; init; }
        public int AssignedCount { get; init; }
        public int UnassignedCount { get; init; }
    }
}