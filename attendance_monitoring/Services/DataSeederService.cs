using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace attendance_monitoring.Services
{
    /// <summary>
    /// Service for seeding initial data into the database.
    /// Uses proper transaction handling to prevent orphaned users.
    /// </summary>
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
                await SeedInstructorsAsync();
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

        private async Task SeedInstructorsAsync()
        {
            // Define instructors data
            var instructorsData = new[]
            {
                new { Lastname = "Comaingking", Firstname = "Jovelyn" },
                new { Lastname = "Francisco", Firstname = "Donald" },
                new { Lastname = "Paran", Firstname = "Jehn Lyn" },
                new { Lastname = "Gelicame", Firstname = "Annalyn" },
                new { Lastname = "Arguzon", Firstname = "Mark John Paul" },
                new { Lastname = "Medio", Firstname = "Elvira" },
                new { Lastname = "Balili", Firstname = "Gerard" }
            };

            // Pre-fetch existing instructors to avoid N+1 queries
            var existingInstructors = await context.Instructors
                .Select(i => new { i.Firstname, i.Lastname })
                .ToListAsync();

            foreach (var instructorData in instructorsData)
            {
                // Check if instructor already exists using the pre-fetched list
                var instructorExists = existingInstructors.Any(i =>
                    i.Firstname == instructorData.Firstname && i.Lastname == instructorData.Lastname);

                if (instructorExists)
                {
                    continue;
                }

                var email = $"{instructorData.Firstname.Replace(" ", ".").ToLower()}.{instructorData.Lastname.ToLower()}@instructor.com";
                
                // Check if user already exists
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    // User exists, check if they have an instructor profile
                    var hasProfile = await context.Instructors.AnyAsync(i => i.UserId == existingUser.Id);
                    if (!hasProfile)
                    {
                        logger.LogWarning("User {Email} exists but has no instructor profile. Creating profile.", email);
                        var instructorForExisting = new Instructor
                        {
                            Firstname = instructorData.Firstname,
                            Lastname = instructorData.Lastname,
                            UserId = existingUser.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        context.Instructors.Add(instructorForExisting);
                        await context.SaveChangesAsync();
                    }
                    existingInstructors.Add(new { Firstname = instructorData.Firstname, Lastname = instructorData.Lastname });
                    continue;
                }

                // Use transaction to ensure atomicity of user + role + profile creation
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Create Identity User
                    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, "DefaultPassword123!");
                    if (!result.Succeeded)
                    {
                        logger.LogError("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        await transaction.RollbackAsync();
                        continue;
                    }

                    // Add role
                    var roleResult = await userManager.AddToRoleAsync(user, "Teacher");
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError("Failed to assign Teacher role to user {Email}: {Errors}", email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        await transaction.RollbackAsync();
                        continue;
                    }

                    // Create Instructor Entity
                    var instructor = new Instructor
                    {
                        Firstname = instructorData.Firstname,
                        Lastname = instructorData.Lastname,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Instructors.Add(instructor);
                    await context.SaveChangesAsync();

                    // Commit the transaction only after all operations succeed
                    await transaction.CommitAsync();
                    
                    // Add to existing instructors list to prevent duplicates in this batch
                    existingInstructors.Add(new { Firstname = instructorData.Firstname, Lastname = instructorData.Lastname });
                    
                    logger.LogInformation("Successfully created instructor {Firstname} {Lastname} with user {Email}", 
                        instructorData.Firstname, instructorData.Lastname, email);
                }
                catch (Exception ex) when (ex.Message.Contains("CK_") || ex.Message.Contains("constraint"))
                {
                    logger.LogError(ex, "Database constraint violation while creating instructor {Email}. Rolling back.", email);
                    await transaction.RollbackAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create instructor {Email}. Rolling back transaction.", email);
                    await transaction.RollbackAsync();
                }
            }

            logger.LogInformation("Seeded instructors.");
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
                    logger.LogWarning("Section {Section} not found for student {Firstname} {Lastname}. Skipping.", 
                        studentData.Section, studentData.Firstname, studentData.Lastname);
                    continue;
                }

                var email = $"{studentData.Firstname.Replace(" ", ".").ToLower()}.{studentData.Lastname.ToLower()}@student.com";
                
                // Check if user already exists
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    // User exists, check if they have a student profile
                    var hasProfile = await context.Students.AnyAsync(s => s.UserId == existingUser.Id);
                    if (!hasProfile)
                    {
                        logger.LogWarning("User {Email} exists but has no student profile. Creating profile.", email);
                        var studentForExisting = new Student
                        {
                            Firstname = studentData.Firstname,
                            Lastname = studentData.Lastname,
                            UserId = existingUser.Id,
                            SectionId = sectionId,
                            IsRegular = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        context.Students.Add(studentForExisting);
                        await context.SaveChangesAsync();
                    }
                    existingStudents.Add(new { Firstname = studentData.Firstname, Lastname = studentData.Lastname });
                    continue;
                }

                // Use transaction to ensure atomicity of user + role + profile creation
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Create Identity User
                    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, "DefaultPassword123!");
                    if (!result.Succeeded)
                    {
                        logger.LogError("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        await transaction.RollbackAsync();
                        continue;
                    }

                    // Add role
                    var roleResult = await userManager.AddToRoleAsync(user, "Student");
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError("Failed to assign Student role to user {Email}: {Errors}", email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        await transaction.RollbackAsync();
                        continue;
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
                    await context.SaveChangesAsync();

                    // Commit the transaction only after all operations succeed
                    await transaction.CommitAsync();
                    
                    // Add to existing students list to prevent duplicates in this batch
                    existingStudents.Add(new { Firstname = studentData.Firstname, Lastname = studentData.Lastname });
                    
                    logger.LogInformation("Successfully created student {Firstname} {Lastname} with user {Email}", 
                        studentData.Firstname, studentData.Lastname, email);
                }
                catch (Exception ex) when (ex.Message.Contains("CK_") || ex.Message.Contains("constraint"))
                {
                    logger.LogError(ex, "Database constraint violation while creating student {Email}. Rolling back.", email);
                    await transaction.RollbackAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create student {Email}. Rolling back transaction.", email);
                    await transaction.RollbackAsync();
                }
            }

            logger.LogInformation("Seeded students.");
        }
    }
}
