using Shared.EWS.DataModel.Request;
using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Project
{
    public class ProjectSearchRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public ProjectStatus? ProjectStatus { get; set; }
        public int? TeamLeadId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
    }
}
