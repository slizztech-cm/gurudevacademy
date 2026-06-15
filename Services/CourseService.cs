using Microsoft.EntityFrameworkCore;
using GurudevDefenceAcademy.Models.Entities;
using GurudevDefenceAcademy.Repositories.Base;

namespace GurudevDefenceAcademy.Services;

public interface ICourseService
{
    Task<List<CourseCategory>> GetCategoriesWithCoursesAsync(bool activeOnly = true);
    Task<List<CourseCategory>> GetCategoriesAsync(bool activeOnly = true);
    Task<CourseCategory?> GetCategoryAsync(int id);
    Task<CourseCategory> CreateCategoryAsync(CourseCategory cat);
    Task UpdateCategoryAsync(CourseCategory cat);
    Task DeleteCategoryAsync(int id);

    Task<List<Course>> GetCoursesAsync(int? categoryId = null);
    Task<Course?> GetCourseAsync(int id);
    Task<Course> CreateCourseAsync(Course course);
    Task UpdateCourseAsync(Course course);
    Task DeleteCourseAsync(int id);
}

public class CourseService(
    IBaseRepository<CourseCategory> catRepo,
    IBaseRepository<Course> courseRepo) : ICourseService
{
    public async Task<List<CourseCategory>> GetCategoriesWithCoursesAsync(bool activeOnly = true)
    {
        var q = catRepo.Query().Include(c => c.Courses.OrderBy(x => x.DisplayOrder)).AsQueryable();
        if (activeOnly) q = q.Where(c => c.IsActive);
        return await q.OrderBy(c => c.DisplayOrder).AsNoTracking().ToListAsync();
    }

    public async Task<List<CourseCategory>> GetCategoriesAsync(bool activeOnly = true)
    {
        var q = catRepo.Query();
        if (activeOnly) q = q.Where(c => c.IsActive);
        return await q.OrderBy(c => c.DisplayOrder).AsNoTracking().ToListAsync();
    }

    public Task<CourseCategory?> GetCategoryAsync(int id) => catRepo.GetByIdAsync(id);
    public Task<CourseCategory> CreateCategoryAsync(CourseCategory cat) => catRepo.AddAsync(cat);
    public Task UpdateCategoryAsync(CourseCategory cat) => catRepo.UpdateAsync(cat);

    public async Task DeleteCategoryAsync(int id)
    {
        var cat = await catRepo.GetByIdAsync(id);
        if (cat is not null) await catRepo.DeleteAsync(cat);
    }

    public async Task<List<Course>> GetCoursesAsync(int? categoryId = null)
    {
        var q = courseRepo.Query().Include(c => c.Category).AsQueryable();
        if (categoryId is not null) q = q.Where(c => c.CategoryId == categoryId);
        return await q.OrderBy(c => c.DisplayOrder).AsNoTracking().ToListAsync();
    }

    public Task<Course?> GetCourseAsync(int id) => courseRepo.GetByIdAsync(id);
    public Task<Course> CreateCourseAsync(Course course) => courseRepo.AddAsync(course);
    public Task UpdateCourseAsync(Course course) => courseRepo.UpdateAsync(course);

    public async Task DeleteCourseAsync(int id)
    {
        var course = await courseRepo.GetByIdAsync(id);
        if (course is not null) await courseRepo.DeleteAsync(course);
    }
}
