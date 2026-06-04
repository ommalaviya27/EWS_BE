namespace Domain.EWS.DataModels.Response.Reports
{
    //  Overview (pie) 
    public class ProjectStatusDistributionResponse
    {
        public int Active { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
    }

    public class ProjectProgressOverviewResponse
    {
        public ProjectStatusDistributionResponse StatusDistribution { get; set; } = new();
    }

    // Paged summary (grid)
    public class ProjectProgressSummaryResponse
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OnHoldTasks { get; set; }
        public double ProgressPercentage { get; set; }
    }
}