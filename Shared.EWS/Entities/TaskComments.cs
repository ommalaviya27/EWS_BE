namespace Shared.EWS.Entities
{
    public class TaskComment : BaseEntity
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public required string Comment { get; set; }

        // Navigation
        public Tasks? Task { get; set; }
        public User? User { get; set; }
    }
}