using Shared.EWS.DataModel.Request;

namespace Domain.EWS.DataModels.Request.User
{
    public class UserPaginationRequest : PaginationRequest
    {
        public string Filter { get; set; } = "all";
        public string? Search { get; set; }
    }
}
