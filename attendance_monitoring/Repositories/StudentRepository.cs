using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Repositories;

public class StudentRepository(ApplicationDbContext context) : IStudentRepository
{
    public async Task<IEnumerable<Student>> GetAllStudentsAsync(PaginationQuery paginationQuery)
    {
        // return await _context.Students.ToListAsync();
        return await context.Students
            .Skip((paginationQuery.PageNumber - 1) * paginationQuery.PageSize)
            .Take(paginationQuery.PageSize)
            .ToListAsync();
    }

    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await context.Students.FindAsync(id);
    }

    public async Task<Student?> GetStudentByUserIdAsync(string userId)
    {
        return await context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Student> CreateStudent(Student student)
    {
        var entry = await context.Students.AddAsync(student);
        return entry.Entity;
    }

    public Task<Student> UpdateStudentAsync(Student student)
    {
        var entry = context.Students.Update(student);
        return Task.FromResult(entry.Entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }
}
