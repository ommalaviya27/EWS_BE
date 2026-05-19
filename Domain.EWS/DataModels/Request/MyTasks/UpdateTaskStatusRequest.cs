using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Tasks
{
    public class UpdateTaskStatusRequest
    {
        public TaskStatuses Status { get; set; }
    }
}
