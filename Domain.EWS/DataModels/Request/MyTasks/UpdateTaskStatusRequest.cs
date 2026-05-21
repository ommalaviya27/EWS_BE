using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.MyTasks
{
    public class UpdateTaskStatusRequest
    {
        public TaskStatuses Status { get; set; }
    }
}
