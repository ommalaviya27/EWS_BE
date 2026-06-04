using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.Reports
{
    public class TaskCompletionSummaryRequest : PaginationRequest
    {
        public string? Search { get; set; }
    }
}