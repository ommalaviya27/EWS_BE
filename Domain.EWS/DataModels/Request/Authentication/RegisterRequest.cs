namespace Domain.EWS.DataModels.Request.Authentication
{
    public class RegisterRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string MobileNumber { get; set; }
        public bool Status { get; set; } = true;
    }
}