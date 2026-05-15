namespace Shared.EWS.DataModel.Response
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResponse<T> Create(
            IEnumerable<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            return new PagedResponse<T>
            {
                Items       = items,
                TotalCount  = totalCount,
                PageNumber  = pageNumber,
                PageSize    = pageSize
            };
        }
    }
}
