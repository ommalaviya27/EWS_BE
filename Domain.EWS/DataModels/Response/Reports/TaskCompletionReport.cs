namespace Domain.EWS.DataModels.Response.Reports
{
    public class TaskStatusDistributionResponse
    {
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int OnHold { get; set; }
        public int Total { get; set; }
    }

    public class TaskPriorityDistributionResponse
    {
        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }
        public int Total { get; set; }
    }

    public class TaskCompletionOverviewResponse
    {
        public TaskStatusDistributionResponse StatusDistribution { get; set; } = new();
        public TaskPriorityDistributionResponse PriorityDistribution { get; set; } = new();
    }

    public class TaskCompletionSummaryItemResponse
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string ProjectName { get; set; } = string.Empty;
    }
}