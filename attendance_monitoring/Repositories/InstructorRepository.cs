using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.Request;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace attendance_monitoring.Repositories;

public class InstructorRepository : IInstructorRepository
{
    private readonly ApplicationDbContext _context;

    public InstructorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery)
    {
        return await _context.Instructors
            .Skip((paginationQuery.PageNumber - 1) * paginationQuery.PageSize)
            .Take(paginationQuery.PageSize)
            .ToListAsync();
    }

    public async Task<Instructor?> GetInstructorByIdAsync(int id)
    {
        return await _context.Instructors.FindAsync(id);
    }

    public async Task<Instructor?> GetInstructorByUserIdAsync(string userId)
    {
        return await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
    }

    public async Task<Instructor> CreateInstructor(Instructor instructor)
    {
        var entry = await _context.Instructors.AddAsync(instructor);
        return entry.Entity;
    }
    
    public async Task<Instructor> UpdateInstructorAsync(Instructor instructor)
    {
        instructor.UpdatedAt = DateTime.UtcNow;
        var entry = _context.Instructors.Update(instructor);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}