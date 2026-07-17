using System;

namespace Leitor.Erp.Pages.Shared;

public class PaginationModel
{
    public const int DefaultPageSize = 20;

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
    public long TotalCount { get; set; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
    public int SkipCount => (PageIndex - 1) * PageSize;
    public long FirstItemNumber => TotalCount == 0 ? 0 : SkipCount + 1;
    public long LastItemNumber => Math.Min(SkipCount + PageSize, TotalCount);
}
