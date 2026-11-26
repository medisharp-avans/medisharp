namespace Server.Models.DTO.Common
{
    /// <summary>One page of a longer list, with the numbers the front-end needs for "page 2 of 7".</summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
