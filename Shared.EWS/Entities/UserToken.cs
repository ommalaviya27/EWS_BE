namespace Shared.EWS.Entities
{
    public class UserToken : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;

        // Navigation property
        public User User { get; set; } = null!;
    }
}
