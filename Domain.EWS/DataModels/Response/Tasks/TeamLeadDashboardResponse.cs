using Domain.EWS.DataModels.Response.Project;

namespace Domain.EWS.DataModels.Response.Tasks
{
    public class TeamLeadDashboardResponse
    {
        public int MyTeamTaskCount    { get; set; }
        public int OverdueTaskCount   { get; set; }
        public int ActiveProjectCount { get; set; }
        public List<GetProjectResponse> ActiveProjects { get; set; } = [];
        public List<GetProjectResponse> CompletedProjects { get; set; } = [];

        public List<GetTaskResponse> RecentTeamTasks { get; set; } = [];
        public int RecentTeamTasksTotalCount { get; set; }

        public List<GetTaskResponse> OverdueTasks { get; set; } = [];
    }
}