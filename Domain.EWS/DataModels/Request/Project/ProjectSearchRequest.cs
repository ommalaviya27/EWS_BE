using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.Project
{
    public class ProjectSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
    }
}