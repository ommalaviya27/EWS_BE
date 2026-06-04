namespace Domain.EWS.DataModels.Response.Reports
{
    public class TopEmployeeTaskResponse
    {
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int OnHold { get; set; }
        public int Pending { get; set; }
        public int Total { get; set; }
    }

    public class EmployeeTaskSummaryResponse
    {
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Pending { get; set; }
        public int OnHold { get; set; }
        public double CompletionRate { get; set; }
    }

    public class EmployeePerformanceReportResponse
    {
        public List<TopEmployeeTaskResponse> TopEmployees { get; set; } = [];
    }
}