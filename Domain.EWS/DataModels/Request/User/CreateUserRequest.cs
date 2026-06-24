namespace Domain.EWS.DataModels.Request.User
{
    public class CreateUserRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string MobileNumber { get; set; }
        public int RoleId { get; set; }
        public int? ReportingId { get; set; }
        public bool Status { get; set; } = true;
    }
}