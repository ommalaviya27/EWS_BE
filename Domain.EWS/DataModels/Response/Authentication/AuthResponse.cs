namespace Domain.EWS.DataModels.Response.Authentication
{
    public class AuthResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
    }
}