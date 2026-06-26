namespace PRN232.LMSSystem.CourseService.Models.Query;

public class QueryParameters
{
    private const int MaxPageSize = 250;
    private int _pageSize = 10;

    public string? Search { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public int Size
    {
        get => PageSize;
        set => PageSize = value;
    }

    public string? Fields { get; set; }
    public string? Expand { get; set; }
}
