using Domain.EWS.DataModels.Response.Tasks;

namespace Domain.EWS.DataModels.Response.MyTasks
{
    public class EmployeeDashboardResponse
    {
        public int AssignedTaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public int UpcomingDeadlineCount { get; set; }

        public List<GetTaskResponse> UpcomingDeadlines { get; set; } = [];
        public List<GetTaskResponse> OnHoldTasks { get; set; } = [];
        public List<GetTaskResponse> CompletedTasks { get; set; } = [];
        public List<GetTaskResponse> OverdueTasks { get; set; } = [];
    }
}