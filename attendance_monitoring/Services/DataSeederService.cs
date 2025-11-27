using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace attendance_monitoring.Services
{
    public class DataSeederService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<DataSeederService> logger)
        : IDataSeederService
    {
        public async Task SeedDataAsync()
        {
            try
            {
                logger.LogInformation("Starting data seeding...");

                await SeedClassroomsAsync();
                await SeedCoursesAndSectionsAsync();
                await SeedStudentsAsync();

                logger.LogInformation("Data seeding completed successfully.");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "A database update error occurred during data seeding: {Message}", ex.Message);
                throw;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "Data seeding was cancelled: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred during data seeding: {Message}", ex.Message);
                throw;
            }
        }

        private async Task SeedClassroomsAsync()
        {
            if (await context.Classrooms.AnyAsync())
            {
                logger.LogInformation("Classrooms already seeded.");
                return;
            }

            var classrooms = new List<Classroom>();

            // Room 101 upto Room 110
            for (int i = 101; i <= 110; i++)
            {
                classrooms.Add(new Classroom { Name = $"Room {i}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            }

            // Room 201 upto Room 209
            for (int i = 201; i <= 209; i++)
            {
                classrooms.Add(new Classroom { Name = $"Room {i}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            }

            // Room 301 only
            classrooms.Add(new Classroom { Name = "Room 301", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            // Laboratories
            classrooms.Add(new Classroom { Name = "Short course laboratory", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            classrooms.Add(new Classroom { Name = "Network Laboratory", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            // Software laboratory 1 upto 5
            for (int i = 1; i <= 5; i++)
            {
                classrooms.Add(new Classroom { Name = $"Software laboratory {i}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            }

            await context.Classrooms.AddRangeAsync(classrooms);
            await context.SaveChangesAsync();
            logger.LogInformation($"Seeded {classrooms.Count} classrooms.");
        }

        private async Task SeedCoursesAndSectionsAsync()
        {
            // Seed Course
            var courseName = "BSCS";
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Name == courseName);
            if (course == null)
            {
                course = new Course { Name = courseName, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                context.Courses.Add(course);
                await context.SaveChangesAsync();
                logger.LogInformation($"Seeded course: {courseName}");
            }

            // Seed Sections
            var sectionNames = new[] { "CS31A", "CS31B", "CS31C" };
            foreach (var sectionName in sectionNames)
            {
                // Check by Name only as it has a unique index
                var section = await context.Sections.FirstOrDefaultAsync(s => s.Name == sectionName);
                if (section == null)
                {
                    section = new Section { Name = sectionName, CourseId = course.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    context.Sections.Add(section);
                }
                else if (section.CourseId != course.Id)
                {
                    logger.LogWarning($"Section {sectionName} exists but belongs to Course {section.CourseId} instead of {course.Id}");
                }
            }
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded sections.");
        }

        private async Task SeedStudentsAsync()
        {
            // Define students data
            var studentsData = new[]
            {
                new { Lastname = "Alicaba", Firstname = "Christian Dave", Section = "CS31A", Course = "BSCS" },
                new { Lastname = "Betonio", Firstname = "Jose Emmanuel", Section = "CS31A", Course = "BSCS" },
                new { Lastname = "Bulahan", Firstname = "Michelle", Section = "CS31A", Course = "BSCS" },
                new { Lastname = "Cando", Firstname = "Manuel", Section = "CS31B", Course = "BSCS" },
                new { Lastname = "Casupanan", Firstname = "John Cez", Section = "CS31C", Course = "BSCS" },
                new { Lastname = "Chavez", Firstname = "Arcgel", Section = "CS31C", Course = "BSCS" },
                new { Lastname = "Cortes", Firstname = "Marc Ejay", Section = "CS31B", Course = "BSCS" },
                new { Lastname = "Francisco", Firstname = "Uzziah Lanz", Section = "CS31A", Course = "BSCS" },
                new { Lastname = "Inot", Firstname = "Nicole Keith", Section = "CS31B", Course = "BSCS" },
                new { Lastname = "Jacinto", Firstname = "Weah Joy", Section = "CS31C", Course = "BSCS" },
                new { Lastname = "Lastimoso", Firstname = "Egin Karl", Section = "CS31C", Course = "BSCS" },
                new { Lastname = "Maguinda", Firstname = "James Ryan", Section = "CS31A", Course = "BSCS" },
                new { Lastname = "Mangubat", Firstname = "Edrian", Section = "CS31B", Course = "BSCS" },
                new { Lastname = "Morales", Firstname = "Stanleigh Jeddro", Section = "CS31C", Course = "BSCS" },
                new { Lastname = "Otadoy", Firstname = "Kent Jay", Section = "CS31A", Course = "BSCS" },
                new { Lastname = "Rosalijos", Firstname = "Jan Nino", Section = "CS31B", Course = "BSCS" },
                new { Lastname = "Sayson", Firstname = "Christian Dave", Section = "CS31B", Course = "BSCS" }, 
                new { Lastname = "Vallecer", Firstname = "Neil Jhonreise", Section = "CS31C", Course = "BSCS" },
                new { Lastname = "Villa", Firstname = "Rajiemae", Section = "CS31B", Course = "BSCS" }
            };

            // Get the BSCS course first
            var bsCsCourse = await context.Courses.FirstOrDefaultAsync(c => c.Name == "BSCS");
            if (bsCsCourse == null)
            {
                logger.LogWarning("BSCS course not found for student seeding.");
                return; // Exit if course doesn't exist
            }

            // Pre-fetch all sections for BSCS course to avoid queries in loop
            var sections = await context.Sections
                .Where(s => s.CourseId == bsCsCourse.Id)
                .ToDictionaryAsync(s => s.Name, s => s.Id);

            // Pre-fetch existing students to avoid N+1 queries
            var existingStudents = await context.Students
                .Select(s => new { s.Firstname, s.Lastname })
                .ToListAsync();

            foreach (var studentData in studentsData)
            {
                // Check if student already exists using the pre-fetched list
                var studentExists = existingStudents.Any(s =>
                    s.Firstname == studentData.Firstname && s.Lastname == studentData.Lastname);

                if (studentExists)
                {
                    continue;
                }

                if (!sections.TryGetValue(studentData.Section, out var sectionId))
                {
                    logger.LogWarning($"Section {studentData.Section} not found for student {studentData.Firstname} {studentData.Lastname}. Skipping.");
                    continue;
                }

                // Create Identity User
                var email = $"{studentData.Firstname.Replace(" ", ".").ToLower()}.{studentData.Lastname.ToLower()}@student.com";
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, "DefaultPassword123!");
                    if (!result.Succeeded)
                    {
                        logger.LogError($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        continue;
                    }
                    await userManager.AddToRoleAsync(user, "Student");
                }

                // Create Student Entity
                var student = new Student
                {
                    Firstname = studentData.Firstname,
                    Lastname = studentData.Lastname,
                    UserId = user.Id,
                    SectionId = sectionId,
                    IsRegular = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Students.Add(student);
                // Add to existing students list to prevent duplicates in this batch
                existingStudents.Add(new { Firstname = studentData.Firstname, Lastname = studentData.Lastname });
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Seeded students.");
        }
    }
}
