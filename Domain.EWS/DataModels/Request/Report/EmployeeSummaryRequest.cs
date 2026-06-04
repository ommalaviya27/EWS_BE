using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.Reports
{
    public class EmployeeSummaryRequest : PaginationRequest
    {
        public string? Search { get; set; }
    }
}