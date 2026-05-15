namespace Shared.EWS.Entities
{
    public class User : BaseEntity
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int RoleId { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string MobileNumber { get; set; }
        public bool status { get; set; } = true;

        // Forgot password (If necessary.....)
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        
        public ICollection<UserToken> Tokens { get; set; } = new List<UserToken>();
    }
}