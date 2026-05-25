using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.Tasks
{
    public class TaskSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
    }
}