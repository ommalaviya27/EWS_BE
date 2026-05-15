using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Project
{
    public class CreateProjectRequest
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ProjectStatus ProjectStatus { get; set; } = ProjectStatus.Active;
    }
}