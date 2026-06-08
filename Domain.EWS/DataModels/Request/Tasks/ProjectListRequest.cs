using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.Tasks
{
    public class ProjectListRequest : PaginationRequest
    {
        public string? Search { get; set; }
    }
}