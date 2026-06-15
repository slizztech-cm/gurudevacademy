using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Data;

namespace GurudevDefenceAcademy.Repositories.Base;

public class BaseRepository<T>(AppDbContext db) : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        => filter is null ? await Set.AsNoTracking().ToListAsync()
                          : await Set.AsNoTracking().Where(filter).ToListAsync();

    public async Task<T?> GetByIdAsync(int id) => await Set.FindAsync(id);

    public async Task<T?> GetOneAsync(Expression<Func<T, bool>> filter)
        => await Set.FirstOrDefaultAsync(filter);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter)
        => await Set.AnyAsync(filter);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        => filter is null ? await Set.CountAsync() : await Set.CountAsync(filter);

    public async Task<T> AddAsync(T entity)
    {
        await Set.AddAsync(entity);
        await Db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        Set.Update(entity);
        await Db.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        Set.Remove(entity);
        await Db.SaveChangesAsync();
    }

    public IQueryable<T> Query() => Set.AsQueryable();

    public Task<int> SaveChangesAsync() => Db.SaveChangesAsync();
}
