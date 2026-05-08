namespace Domain.EWS.DataModels.Response.Authentication
{
    public class AuthResponse
    {
        public required string Token { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
    }
}