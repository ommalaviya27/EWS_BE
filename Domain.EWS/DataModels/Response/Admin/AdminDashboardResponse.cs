using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;

namespace Domain.EWS.DataModels.Response.Admin
{
    public class AdminDashboardResponse
    {
        public int TotalEmployees { get; set; }
        public int TotalProjects { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        public List<GetProjectResponse> OverdueProjects { get; set; } = [];
        public List<GetProjectResponse> RecentCompletedProjects { get; set; } = [];
        public List<GetTaskResponse> OverdueTasks { get; set; } = [];
    }
}