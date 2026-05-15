namespace Shared.EWS.DataModel.Request
{
    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        private static readonly int[] AllowedPageSizes = { 5, 10, 20, 100 };

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = AllowedPageSizes.Contains(value) ? value : 10;
        }
    }
}