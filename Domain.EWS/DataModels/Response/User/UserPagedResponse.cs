using Shared.EWS.DataModel.Response;

namespace Domain.EWS.DataModels.Response.User
{
    public class UserPagedResponse
    {
        public IEnumerable<GetUserResponse> Items { get; init; } = Enumerable.Empty<GetUserResponse>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        
        public UserSummary Summary { get; init; } = new();

        public static UserPagedResponse From(PagedResponse<GetUserResponse> paged, UserSummary summary)
        {
            return new UserPagedResponse
            {
                Items         = paged.Items,
                TotalCount    = paged.TotalCount,
                PageNumber    = paged.PageNumber,
                PageSize      = paged.PageSize,
                Summary       = summary,
            };
        }
    }
}
