using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.Request;

public class PaginationQuery
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (must be greater than 0)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size (must be between 1 and MaxPageSize)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? 1 : value);
    }
}
