namespace Domain.EWS.DataModels.Request.User
{
    public class UpdateUserRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string MobileNumber { get; set; }
        public int RoleId { get; set; }   // 1=Admin, 2=Team Lead, 3=Employee
        public int? TeamLeadId { get; set; }
        public bool Status { get; set; }
    }
}