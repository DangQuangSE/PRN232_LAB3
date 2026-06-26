using System.Linq.Expressions;

namespace PRN232.LMSSystem.CourseService.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        List<string>? includeProperties = null,
        int? page = null,
        int? pageSize = null);

    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

    Task<T?> GetByIdAsync(object id, List<string>? includeProperties = null);

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);

    Task SaveAsync();
}
