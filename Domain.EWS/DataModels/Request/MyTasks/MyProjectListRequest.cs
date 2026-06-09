using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.MyTasks
{
    public class MyProjectListRequest : PaginationRequest
    {
        public string? Search { get; set; }
    }
}
