using System.Linq.Expressions;

namespace GurudevDefenceAcademy.Services.Base;

// Generic business-layer contract that sits on top of IBaseRepository.
public interface IBaseService<T> where T : class
{
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetOneAsync(Expression<Func<T, bool>> filter);
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
}
