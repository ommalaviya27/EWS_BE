namespace Shared.EWS.Entities
{
    public class ProjectMember
    {
        public int ProjectMemberId { get; set; }
        public Guid ProjectId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Projects? Project { get; set; }
        public User? User { get; set; }
    }
}
