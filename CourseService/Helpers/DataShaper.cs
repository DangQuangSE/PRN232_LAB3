using System.Dynamic;
using System.Reflection;

namespace PRN232.LMSSystem.CourseService.Helpers;

public interface IDataShaper<T>
{
    IEnumerable<ExpandoObject> ShapeData(IEnumerable<T> entities, string? fieldsString);
    ExpandoObject ShapeData(T entity, string? fieldsString);
}

public class DataShaper<T> : IDataShaper<T>
{
    private readonly PropertyInfo[] _properties;

    public DataShaper()
    {
        _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public IEnumerable<ExpandoObject> ShapeData(IEnumerable<T> entities, string? fieldsString)
    {
        var requiredProperties = GetRequiredProperties(fieldsString);
        return entities.Select(entity => FetchDataForEntity(entity, requiredProperties)).ToList();
    }

    public ExpandoObject ShapeData(T entity, string? fieldsString)
    {
        var requiredProperties = GetRequiredProperties(fieldsString);
        return FetchDataForEntity(entity, requiredProperties);
    }

    private List<PropertyInfo> GetRequiredProperties(string? fieldsString)
    {
        var requiredProperties = new List<PropertyInfo>();

        if (!string.IsNullOrWhiteSpace(fieldsString))
        {
            var fields = fieldsString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var field in fields)
            {
                var propertyName = field.Trim();
                var property = _properties.FirstOrDefault(pi => pi.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

                if (property != null)
                {
                    requiredProperties.Add(property);
                }
            }
        }
        else
        {
            requiredProperties = _properties.ToList();
        }

        return requiredProperties;
    }

    private ExpandoObject FetchDataForEntity(T entity, IEnumerable<PropertyInfo> requiredProperties)
    {
        var shapedObject = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)shapedObject;

        foreach (var property in requiredProperties)
        {
            var propertyValue = property.GetValue(entity);
            dictionary.Add(property.Name, propertyValue);
        }

        return shapedObject;
    }
}
