using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Models
{
    public class PagedResponse<T>
    {
        public ICollection<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
