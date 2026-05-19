namespace Shared.EWS.Entities
{
    public class TaskAttachment : BaseEntity
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public required string FileName { get; set; }
        public required string FileUrl { get; set; }
        public long FileSize { get; set; }

        // Navigation
        public Tasks? Task { get; set; }
        public User? User { get; set; }
    }
}