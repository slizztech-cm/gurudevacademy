using System.Linq.Expressions;

namespace GurudevDefenceAcademy.Repositories.Base;

// Generic data-access contract. One per entity via DI.
public interface IBaseRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetOneAsync(Expression<Func<T, bool>> filter);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    IQueryable<T> Query();
    Task<int> SaveChangesAsync();
}
