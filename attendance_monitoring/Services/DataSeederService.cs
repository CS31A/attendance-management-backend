using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

                // Toggle: Uncomment the line below to delete all seeded data before re-seeding
                // await DeleteAllSeededDataAsync();

                await SeedAdminAsync();
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

        /// <summary>
        /// Deletes only the seeded data from the database, preserving any user-created data.
        /// Uses specific filters to target only known seeded records.
        /// </summary>
        private async Task DeleteAllSeededDataAsync()
        {
            logger.LogInformation("Deleting seeded data only...");

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Define seeded data identifiers
                    var seededAdminEmail = "admin@attendance.com";
                    var seededCourseName = "Bachelor of Science in Computer Science";
                    var seededSectionNames = new[] { "CS31A", "CS31B", "CS31C" };
                    
                    // Seeded instructor data (firstname.lastname@instructor.com pattern)
                    var seededInstructorEmails = new[]
                    {
                        "jovelyn.comaingking@instructor.com",
                        "donald.francisco@instructor.com",
                        "jehn.lyn.paran@instructor.com",
                        "annalyn.gelicame@instructor.com",
                        "mark.john.paul.arguzon@instructor.com",
                        "elvira.medio@instructor.com",
                        "gerard.balili@instructor.com"
                    };
                    
                    // Seeded student data (firstname.lastname@student.com pattern)
                    var seededStudentEmails = new[]
                    {
                        "christian.dave.alicaba@student.com",
                        "jose.emmanuel.betonio@student.com",
                        "michelle.bulahan@student.com",
                        "manuel.cando@student.com",
                        "john.cez.casupanan@student.com",
                        "arcgel.chavez@student.com",
                        "marc.ejay.cortes@student.com",
                        "uzziah.lanz.francisco@student.com",
                        "nicole.keith.inot@student.com",
                        "weah.joy.jacinto@student.com",
                        "egin.karl.lastimoso@student.com",
                        "james.ryan.maguinda@student.com",
                        "edrian.mangubat@student.com",
                        "stanleigh.jeddro.morales@student.com",
                        "kent.jay.otadoy@student.com",
                        "jan.nino.rosalijos@student.com",
                        "christian.dave.sayson@student.com",
                        "neil.jhonreise.vallecer@student.com",
                        "rajiemae.villa@student.com"
                    };
                    
                    // Seeded classroom names
                    var seededClassroomNames = new List<string>();
                    for (int i = 101; i <= 110; i++) seededClassroomNames.Add($"Room {i}");
                    for (int i = 201; i <= 209; i++) seededClassroomNames.Add($"Room {i}");
                    seededClassroomNames.Add("Room 301");
                    seededClassroomNames.Add("Short course laboratory");
                    seededClassroomNames.Add("Network Laboratory");
                    for (int i = 1; i <= 5; i++) seededClassroomNames.Add($"Software laboratory {i}");

                    // Get seeded user IDs for targeted deletion
                    var allSeededEmails = new List<string> { seededAdminEmail };
                    allSeededEmails.AddRange(seededInstructorEmails);
                    allSeededEmails.AddRange(seededStudentEmails);
                    
                    var seededUserIds = await context.Users
                        .Where(u => allSeededEmails.Contains(u.Email!))
                        .Select(u => u.Id)
                        .ToListAsync();

                    // Delete in order to respect foreign key constraints
                    // Start with the most dependent tables first

                    // Delete attendance records for seeded students only
                    var seededStudentIds = await context.Students
                        .Where(s => seededUserIds.Contains(s.UserId))
                        .Select(s => s.Id)
                        .ToListAsync();
                    
                    if (seededStudentIds.Any())
                    {
                        var attendanceRecordsDeleted = await context.AttendanceRecords
                            .Where(ar => seededStudentIds.Contains(ar.StudentId))
                            .ExecuteDeleteAsync();
                        logger.LogInformation("Deleted {Count} seeded attendance records.", attendanceRecordsDeleted);
                    }

                    // Get seeded section IDs
                    var seededSectionIds = await context.Sections
                        .Where(s => seededSectionNames.Contains(s.Name))
                        .Select(s => s.Id)
                        .ToListAsync();

                    // Delete QR codes for sessions related to seeded sections
                    if (seededSectionIds.Any())
                    {
                        var seededSessionIds = await context.Sessions
                            .Where(s => s.Schedule != null && seededSectionIds.Contains(s.Schedule.SectionId))
                            .Select(s => s.Id)
                            .ToListAsync();
                        
                        if (seededSessionIds.Any())
                        {
                            var qrCodesDeleted = await context.QrCodes
                                .Where(qr => seededSessionIds.Contains(qr.SessionId))
                                .ExecuteDeleteAsync();
                            logger.LogInformation("Deleted {Count} seeded QR codes.", qrCodesDeleted);

                            var sessionsDeleted = await context.Sessions
                                .Where(s => seededSessionIds.Contains(s.Id))
                                .ExecuteDeleteAsync();
                            logger.LogInformation("Deleted {Count} seeded sessions.", sessionsDeleted);
                        }
                    }

                    // Delete student enrollments for seeded students
                    if (seededStudentIds.Any())
                    {
                        var studentEnrollmentsDeleted = await context.StudentEnrollments
                            .Where(se => seededStudentIds.Contains(se.StudentId))
                            .ExecuteDeleteAsync();
                        logger.LogInformation("Deleted {Count} seeded student enrollments.", studentEnrollmentsDeleted);
                    }

                    // Delete schedules for seeded sections
                    if (seededSectionIds.Any())
                    {
                        var schedulesDeleted = await context.Schedules
                            .Where(s => seededSectionIds.Contains(s.SectionId))
                            .ExecuteDeleteAsync();
                        logger.LogInformation("Deleted {Count} seeded schedules.", schedulesDeleted);
                    }

                    // Delete seeded students
                    var studentsDeleted = await context.Students
                        .Where(s => seededUserIds.Contains(s.UserId))
                        .ExecuteDeleteAsync();
                    logger.LogInformation("Deleted {Count} seeded students.", studentsDeleted);

                    // Delete seeded instructors
                    var instructorsDeleted = await context.Instructors
                        .Where(i => seededUserIds.Contains(i.UserId))
                        .ExecuteDeleteAsync();
                    logger.LogInformation("Deleted {Count} seeded instructors.", instructorsDeleted);

                    // Delete seeded admin
                    var adminsDeleted = await context.Admins
                        .Where(a => seededUserIds.Contains(a.UserId))
                        .ExecuteDeleteAsync();
                    logger.LogInformation("Deleted {Count} seeded admins.", adminsDeleted);

                    // Delete seeded sections
                    var sectionsDeleted = await context.Sections
                        .Where(s => seededSectionNames.Contains(s.Name))
                        .ExecuteDeleteAsync();
                    logger.LogInformation("Deleted {Count} seeded sections.", sectionsDeleted);

                    // Delete seeded course
                    var coursesDeleted = await context.Courses
                        .Where(c => c.Name == seededCourseName)
                        .ExecuteDeleteAsync();
                    logger.LogInformation("Deleted {Count} seeded courses.", coursesDeleted);

                    // Delete seeded classrooms
                    var classroomsDeleted = await context.Classrooms
                        .Where(c => seededClassroomNames.Contains(c.Name))
                        .ExecuteDeleteAsync();
                    logger.LogInformation("Deleted {Count} seeded classrooms.", classroomsDeleted);

                    // Delete refresh tokens for seeded users only (BlacklistedTokens don't have UserId)
                    if (seededUserIds.Any())
                    {
                        var refreshTokensDeleted = await context.RefreshTokens
                            .Where(rt => seededUserIds.Contains(rt.UserId))
                            .ExecuteDeleteAsync();
                        logger.LogInformation("Deleted {Count} seeded refresh tokens.", refreshTokensDeleted);
                    }

                    // Delete ASP.NET Identity data for seeded users only
                    // Using parameterized queries to avoid SQL injection
                    foreach (var userId in seededUserIds)
                    {
                        await context.Database.ExecuteSqlAsync($"DELETE FROM AspNetUserRoles WHERE UserId = {userId}");
                        await context.Database.ExecuteSqlAsync($"DELETE FROM AspNetUserLogins WHERE UserId = {userId}");
                        await context.Database.ExecuteSqlAsync($"DELETE FROM AspNetUserClaims WHERE UserId = {userId}");
                        await context.Database.ExecuteSqlAsync($"DELETE FROM AspNetUserTokens WHERE UserId = {userId}");
                        await context.Database.ExecuteSqlAsync($"DELETE FROM AspNetUsers WHERE Id = {userId}");
                    }
                    logger.LogInformation("Deleted {Count} seeded ASP.NET Identity users.", seededUserIds.Count);

                    // Note: We keep AspNetRoles as they are configuration, not seeded user data
                    // Note: We don't delete Subjects as they are not seeded by this service
                    // Note: BlacklistedTokens don't have UserId, they expire naturally

                    await transaction.CommitAsync();
                    logger.LogInformation("Successfully deleted all seeded data. Non-seeded data has been preserved.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete seeded data. Rolling back transaction.");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task SeedAdminAsync()
        {
            const string adminEmail = "admin@attendance.com";
            const string adminPassword = "Admin@123!";
            const string adminFirstname = "System";
            const string adminLastname = "Administrator";

            // Check if admin already exists
            var existingAdmin = await context.Admins.AnyAsync(a => a.Firstname == adminFirstname && a.Lastname == adminLastname);
            if (existingAdmin)
            {
                logger.LogInformation("Admin already seeded.");
                return;
            }

            // Check if user already exists
            var existingUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingUser != null)
            {
                // User exists, check if they have an admin profile
                var hasProfile = await context.Admins.AnyAsync(a => a.UserId == existingUser.Id);
                if (!hasProfile)
                {
                    logger.LogWarning("User {Email} exists but has no admin profile. Creating profile.", adminEmail);
                    var adminForExisting = new Admin
                    {
                        Firstname = adminFirstname,
                        Lastname = adminLastname,
                        UserId = existingUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Admins.Add(adminForExisting);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Created admin profile for existing user {Email}.", adminEmail);
                }
                return;
            }

            // Use execution strategy to handle retries properly with transactions
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Create Identity User
                    var user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, adminPassword);
                    if (!result.Succeeded)
                    {
                        logger.LogError("Failed to create admin user {Email}: {Errors}", adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Add Admin role
                    var roleResult = await userManager.AddToRoleAsync(user, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError("Failed to assign Admin role to user {Email}: {Errors}", adminEmail, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Create Admin Entity
                    var admin = new Admin
                    {
                        Firstname = adminFirstname,
                        Lastname = adminLastname,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Admins.Add(admin);
                    await context.SaveChangesAsync();

                    // Commit the transaction only after all operations succeed
                    await transaction.CommitAsync();

                    logger.LogInformation("Successfully created admin {Firstname} {Lastname} with user {Email}",
                        adminFirstname, adminLastname, adminEmail);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create admin {Email}. Rolling back transaction.", adminEmail);
                    await transaction.RollbackAsync();
                }
            });
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
            logger.LogInformation("Seeded {Count} classrooms.", classrooms.Count);
        }

        private async Task SeedCoursesAndSectionsAsync()
        {
            // Seed Course
            var courseName = "Bachelor of Science in Computer Science";
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Name == courseName);
            if (course == null)
            {
                course = new Course { Name = courseName, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                context.Courses.Add(course);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded course: {CourseName}", courseName);
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
                    logger.LogWarning("Section {SectionName} exists but belongs to Course {CurrentCourseId} instead of {ExpectedCourseId}", sectionName, section.CourseId, course.Id);
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
                    existingInstructors.Add(new { Firstname = (string?)instructorData.Firstname, Lastname = (string?)instructorData.Lastname });
                    continue;
                }

                // Use execution strategy to handle retries properly with transactions
                var strategy = context.Database.CreateExecutionStrategy();
                var instructorFirstname = instructorData.Firstname;
                var instructorLastname = instructorData.Lastname;
                
                await strategy.ExecuteAsync(async () =>
                {
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
                            return;
                        }

                        // Add role
                        var roleResult = await userManager.AddToRoleAsync(user, RoleConstants.Instructor);
                        if (!roleResult.Succeeded)
                        {
                            logger.LogError("Failed to assign Instructor role to user {Email}: {Errors}", email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            await transaction.RollbackAsync();
                            return;
                        }

                        // Create Instructor Entity
                        var instructor = new Instructor
                        {
                            Firstname = instructorFirstname,
                            Lastname = instructorLastname,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        context.Instructors.Add(instructor);
                        await context.SaveChangesAsync();

                        // Commit the transaction only after all operations succeed
                        await transaction.CommitAsync();

                        logger.LogInformation("Successfully created instructor {Firstname} {Lastname} with user {Email}",
                            instructorFirstname, instructorLastname, email);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create instructor {Email}. Rolling back transaction.", email);
                        await transaction.RollbackAsync();
                    }
                });
                
                // Add to existing instructors list to prevent duplicates in this batch
                existingInstructors.Add(new { Firstname = (string?)instructorFirstname, Lastname = (string?)instructorLastname });
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

            // Get the course first
            var bsCsCourse = await context.Courses.FirstOrDefaultAsync(c => c.Name == "Bachelor of Science in Computer Science");
            if (bsCsCourse == null)
            {
                logger.LogWarning("Bachelor of Science in Computer Science course not found for student seeding.");
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

                // Use execution strategy to handle retries properly with transactions
                var strategy = context.Database.CreateExecutionStrategy();
                var studentFirstname = studentData.Firstname;
                var studentLastname = studentData.Lastname;
                var studentSectionId = sectionId;
                
                await strategy.ExecuteAsync(async () =>
                {
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
                            return;
                        }

                        // Add role
                        var roleResult = await userManager.AddToRoleAsync(user, "Student");
                        if (!roleResult.Succeeded)
                        {
                            logger.LogError("Failed to assign Student role to user {Email}: {Errors}", email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            await transaction.RollbackAsync();
                            return;
                        }

                        // Create Student Entity
                        var student = new Student
                        {
                            Firstname = studentFirstname,
                            Lastname = studentLastname,
                            UserId = user.Id,
                            SectionId = studentSectionId,
                            IsRegular = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        context.Students.Add(student);
                        await context.SaveChangesAsync();

                        // Commit the transaction only after all operations succeed
                        await transaction.CommitAsync();

                        logger.LogInformation("Successfully created student {Firstname} {Lastname} with user {Email}",
                            studentFirstname, studentLastname, email);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to create student {Email}. Rolling back transaction.", email);
                        await transaction.RollbackAsync();
                    }
                });
                
                // Add to existing students list to prevent duplicates in this batch
                existingStudents.Add(new { Firstname = studentFirstname, Lastname = studentLastname });
            }

            logger.LogInformation("Seeded students.");
        }
    }
}
