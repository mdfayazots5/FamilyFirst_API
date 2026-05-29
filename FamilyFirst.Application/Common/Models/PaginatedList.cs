namespace FamilyFirst.Application.Common.Models;

public sealed class PaginatedList<T>
{
    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize);
    }

    public IReadOnlyCollection<T> Items { get; }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalPages { get; }

    public int TotalCount { get; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PaginatedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var items = source.ToArray();
        var pageItems = pageSize <= 0
            ? Array.Empty<T>()
            : items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray();

        return new PaginatedList<T>(pageItems, items.Length, pageNumber, pageSize);
    }
}
