using Shared.EWS.DataModel.Request;
using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.MyTasks
{
    public class MyTaskSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public TaskStatuses? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
    }
}