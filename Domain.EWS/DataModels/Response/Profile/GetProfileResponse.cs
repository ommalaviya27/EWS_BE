namespace Domain.EWS.DataModels.Response.Profile
{
    public class GetProfileResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? ReportingPersonName { get; set; }
    }
}