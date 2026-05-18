using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Tasks
{
    public class UpdateTaskRequest
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public Guid ProjectId { get; set; }
        public int AssignedToUserId { get; set; }
        public DateTime DueDate { get; set; }
        public TaskStatuses Status { get; set; } = TaskStatuses.Pending;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    }
}