using System.Linq.Dynamic.Core;

namespace PRN232.LMSSystem.CourseService.Helpers;

public static class QueryHelper
{
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string? sortString)
    {
        if (string.IsNullOrWhiteSpace(sortString))
        {
            return source;
        }

        var sortParts = sortString.Split(',');
        var orderExpressions = new List<string>();

        foreach (var part in sortParts)
        {
            var trimmedPart = part.Trim();
            if (string.IsNullOrEmpty(trimmedPart)) continue;

            bool descending = trimmedPart.StartsWith("-");
            var propertyName = descending ? trimmedPart.Substring(1).Trim() : trimmedPart;

            if (string.IsNullOrEmpty(propertyName)) continue;
            
            var titleCasePropertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);

            orderExpressions.Add($"{titleCasePropertyName} {(descending ? "descending" : "ascending")}");
        }

        var orderQuery = string.Join(", ", orderExpressions);
        if (string.IsNullOrWhiteSpace(orderQuery))
        {
            return source;
        }

        return source.OrderBy(orderQuery);
    }
}
