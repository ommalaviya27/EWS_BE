namespace Domain.EWS.DataModels.Request.MyTasks
{
    public class UpdateTaskCommentRequest
    {
        public int TaskId { get; set; }
        public required string Comment { get; set; }
    }
}