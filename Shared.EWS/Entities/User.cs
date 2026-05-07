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
        public bool status { get; set; }
    }
}