using System;

namespace TB.DanceDance.API.Contracts.Models
{
    public class PagedRequest
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;

        public int NormalizedPage => Math.Max(Page, 1);

        public int NormalizedPageSize => Math.Clamp(PageSize, 1, MaxPageSize);
    }
}