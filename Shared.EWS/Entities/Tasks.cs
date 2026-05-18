using Shared.EWS.Enums;

namespace Shared.EWS.Entities
{
    public class Tasks : BaseEntity
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public Guid ProjectId { get; set; }
        public int AssignedToUserId { get; set; }
        public int AssignedByUserId { get; set; } 
        public TaskStatuses TaskStatus { get; set; } = TaskStatuses.Pending;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime DueDate { get; set; }

        // Navigation
        public Projects? Project { get; set; }
        public User? AssignedTo { get; set; }
        public User? AssignedBy { get; set; }
    }
}
