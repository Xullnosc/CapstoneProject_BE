using System;
using System.Collections.Generic;

namespace BusinessObjects.DTOs
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public PagedResult() { }

        public PagedResult(List<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PageIndex = pageIndex;
            PageSize = pageSize > 0 ? pageSize : 10;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            HasNextPage = PageIndex < TotalPages;
            HasPreviousPage = PageIndex > 1;
        }
    }
}
