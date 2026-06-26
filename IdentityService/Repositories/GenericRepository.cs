using Microsoft.EntityFrameworkCore;
using PRN232.LMSSystem.IdentityService.Data;
using PRN232.LMSSystem.IdentityService.Interfaces;
using System.Linq.Expressions;

namespace PRN232.LMSSystem.IdentityService.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly IdentityDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(IdentityDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        List<string>? includeProperties = null,
        int? page = null,
        int? pageSize = null)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (includeProperties != null)
        {
            foreach (var includeProperty in includeProperties)
            {
                if (!string.IsNullOrWhiteSpace(includeProperty))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            int skip = (page.Value - 1) * pageSize.Value;
            query = query.Skip(skip).Take(pageSize.Value);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        IQueryable<T> query = _dbSet;
        if (filter != null)
        {
            query = query.Where(filter);
        }
        return await query.CountAsync();
    }

    public virtual async Task<T?> GetByIdAsync(object id, List<string>? includeProperties = null)
    {
        if (includeProperties != null && includeProperties.Any())
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            var primaryKey = entityType?.FindPrimaryKey();
            var keyName = primaryKey?.Properties.Select(x => x.Name).FirstOrDefault();
            
            if (keyName != null)
            {
                IQueryable<T> query = _dbSet;
                foreach (var includeProperty in includeProperties)
                {
                    if (!string.IsNullOrWhiteSpace(includeProperty))
                    {
                        query = query.Include(includeProperty.Trim());
                    }
                }
                
                var parameter = Expression.Parameter(typeof(T), "e");
                var property = Expression.Property(parameter, keyName);
                var targetKeyProp = primaryKey!.Properties[0];
                var targetType = targetKeyProp.ClrType;
                var convertedId = Convert.ChangeType(id, targetType);
                
                var equal = Expression.Equal(property, Expression.Constant(convertedId));
                var lambda = Expression.Lambda<Func<T, bool>>(equal, parameter);
                
                return await query.FirstOrDefaultAsync(lambda);
            }
        }
        
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public virtual void Delete(T entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        _dbSet.Remove(entity);
    }

    public virtual async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}
