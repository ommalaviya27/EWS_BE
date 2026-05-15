using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Response.Project
{
    public class GetProjectResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int UserId { get; set; }
        public ProjectStatus ProjectStatus { get; set; } = ProjectStatus.Active;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } 
    }
}