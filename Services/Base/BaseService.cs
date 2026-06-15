using System.Linq.Expressions;
using GurudevDefenceAcademy.Repositories.Base;

namespace GurudevDefenceAcademy.Services.Base;

public class BaseService<T>(IBaseRepository<T> repo) : IBaseService<T> where T : class
{
    protected readonly IBaseRepository<T> Repo = repo;

    public virtual Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        => Repo.GetAllAsync(filter);

    public virtual Task<T?> GetByIdAsync(int id) => Repo.GetByIdAsync(id);

    public virtual Task<T?> GetOneAsync(Expression<Func<T, bool>> filter)
        => Repo.GetOneAsync(filter);

    public virtual Task<T> CreateAsync(T entity) => Repo.AddAsync(entity);

    public virtual Task UpdateAsync(T entity) => Repo.UpdateAsync(entity);

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await Repo.GetByIdAsync(id);
        if (entity is not null) await Repo.DeleteAsync(entity);
    }

    public virtual Task<bool> ExistsAsync(Expression<Func<T, bool>> filter)
        => Repo.AnyAsync(filter);
}
