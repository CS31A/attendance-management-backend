using attendance_monitoring.Classes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace attendance_monitoring.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Instructor> Instructors { get; set; } = null!;
        public DbSet<Section> Sections { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Classroom> Classrooms { get; set; } = null!;
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Schedules> Schedules { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; } = null!;
        public DbSet<QrCode> QrCodes { get; set; } = null!;
        public DbSet<StudentEnrollment> StudentEnrollments { get; set; } = null!;
        public DbSet<Session> Sessions { get; set; } = null!;
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;
        public DbSet<Fingerprint> Fingerprints { get; set; } = null!;
        public DbSet<FingerprintDevice> FingerprintDevices { get; set; } = null!;
        public DbSet<FingerprintScanEvent> FingerprintScanEvents { get; set; } = null!;
        public DbSet<FingerprintEnrollmentSession> FingerprintEnrollmentSessions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure unique index for email in AspNetUsers table
            // This prevents duplicate emails at the database level to avoid race conditions
            builder.Entity<IdentityUser>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("IX_AspNetUsers_NormalizedEmail")
                .HasFilter("[NormalizedEmail] IS NOT NULL");

            // Configure index for RefreshToken TokenHash for fast lookups
            builder.Entity<RefreshToken>()
                .HasIndex(r => r.TokenHash)
                .IsUnique();

            var studentUuid = builder.Entity<Student>()
                .Property(s => s.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            builder.Entity<Student>()
                .HasIndex(s => s.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Students_Uuid");

            var instructorUuid = builder.Entity<Instructor>()
                .Property(i => i.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            builder.Entity<Instructor>()
                .HasIndex(i => i.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Instructors_Uuid");

            var adminUuid = builder.Entity<Admin>()
                .Property(a => a.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var courseUuid = builder.Entity<Course>()
                .Property(c => c.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var subjectUuid = builder.Entity<Subject>()
                .Property(s => s.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var sectionUuid = builder.Entity<Section>()
                .Property(s => s.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var classroomUuid = builder.Entity<Classroom>()
                .Property(c => c.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var scheduleUuid = builder.Entity<Schedules>()
                .Property(s => s.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var studentEnrollmentUuid = builder.Entity<StudentEnrollment>()
                .Property(se => se.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var sessionUuid = builder.Entity<Session>()
                .Property(s => s.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var attendanceRecordUuid = builder.Entity<AttendanceRecord>()
                .Property(a => a.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var qrCodeUuid = builder.Entity<QrCode>()
                .Property(q => q.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var fingerprintUuid = builder.Entity<Fingerprint>()
                .Property(f => f.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var fingerprintDeviceUuid = builder.Entity<FingerprintDevice>()
                .Property(d => d.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var fingerprintEnrollmentSessionUuid = builder.Entity<FingerprintEnrollmentSession>()
                .Property(e => e.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            var fingerprintScanEventUuid = builder.Entity<FingerprintScanEvent>()
                .Property(e => e.Uuid)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd();

            builder.Entity<Admin>()
                .HasIndex(a => a.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Admins_Uuid");

            builder.Entity<Course>()
                .HasIndex(c => c.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Courses_Uuid");

            builder.Entity<Subject>()
                .HasIndex(s => s.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Subjects_Uuid");

            builder.Entity<Section>()
                .HasIndex(s => s.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Sections_Uuid");

            builder.Entity<Classroom>()
                .HasIndex(c => c.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Classrooms_Uuid");

            builder.Entity<Schedules>()
                .HasIndex(s => s.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Schedules_Uuid");

            builder.Entity<StudentEnrollment>()
                .HasIndex(se => se.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_StudentEnrollments_Uuid");

            builder.Entity<Session>()
                .HasIndex(s => s.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Sessions_Uuid");

            builder.Entity<AttendanceRecord>()
                .HasIndex(a => a.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_AttendanceRecords_Uuid");

            builder.Entity<QrCode>()
                .HasIndex(q => q.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_QrCodes_Uuid");

            builder.Entity<Fingerprint>()
                .HasIndex(f => f.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_Fingerprints_Uuid");

            builder.Entity<FingerprintDevice>()
                .HasIndex(d => d.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_FingerprintDevices_Uuid");

            builder.Entity<FingerprintEnrollmentSession>()
                .HasIndex(e => e.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_FingerprintEnrollmentSessions_Uuid");

            builder.Entity<FingerprintScanEvent>()
                .HasIndex(e => e.Uuid)
                .IsUnique()
                .HasDatabaseName("IX_FingerprintScanEvents_Uuid");

            // Configure Schedules relationships
            builder.Entity<Schedules>()
                .HasOne(s => s.Subject)
                .WithMany()
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedules>()
                .HasOne(s => s.Section)
                .WithMany()
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedules>()
                .HasOne(s => s.Classroom)
                .WithMany()
                .HasForeignKey(s => s.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedules>()
                .HasOne(s => s.Instructor)
                .WithMany()
                .HasForeignKey(s => s.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Student-Section relationship (primary/home section)
            builder.Entity<Student>()
                .HasOne(s => s.Section)
                .WithMany()
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure StudentEnrollment relationships (additional enrollments)
            builder.Entity<StudentEnrollment>()
                .HasOne(se => se.Student)
                .WithMany(s => s.AdditionalEnrollments)
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentEnrollment>()
                .HasOne(se => se.Section)
                .WithMany(s => s.StudentEnrollments)
                .HasForeignKey(se => se.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentEnrollment>()
                .HasOne(se => se.Subject)
                .WithMany(s => s.StudentEnrollments)
                .HasForeignKey(se => se.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Session relationships
            builder.Entity<Session>()
                .HasOne(s => s.Schedule)
                .WithMany()
                .HasForeignKey(s => s.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Session>()
                .HasOne(s => s.ActualRoom)
                .WithMany()
                .HasForeignKey(s => s.ActualRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Session>()
                .HasOne(s => s.InstructorWhoStarted)
                .WithMany()
                .HasForeignKey(s => s.StartedBy)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Session>()
                .HasOne(s => s.InstructorWhoEnded)
                .WithMany()
                .HasForeignKey(s => s.EndedBy)
                .OnDelete(DeleteBehavior.Restrict);

            if (Database.IsSqlServer())
            {
                studentUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                instructorUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                adminUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                courseUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                subjectUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                sectionUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                classroomUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                scheduleUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                studentEnrollmentUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                sessionUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                attendanceRecordUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                qrCodeUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                fingerprintUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                fingerprintDeviceUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                fingerprintEnrollmentSessionUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
                fingerprintScanEventUuid.HasDefaultValueSql("NEWSEQUENTIALID()");
            }
            else
            {
                // Non-SQL Server test providers cannot execute NEWSEQUENTIALID(), so generate GUIDs client-side.
                studentUuid.HasValueGenerator<GuidValueGenerator>();
                instructorUuid.HasValueGenerator<GuidValueGenerator>();
                adminUuid.HasValueGenerator<GuidValueGenerator>();
                courseUuid.HasValueGenerator<GuidValueGenerator>();
                subjectUuid.HasValueGenerator<GuidValueGenerator>();
                sectionUuid.HasValueGenerator<GuidValueGenerator>();
                classroomUuid.HasValueGenerator<GuidValueGenerator>();
                scheduleUuid.HasValueGenerator<GuidValueGenerator>();
                studentEnrollmentUuid.HasValueGenerator<GuidValueGenerator>();
                sessionUuid.HasValueGenerator<GuidValueGenerator>();
                attendanceRecordUuid.HasValueGenerator<GuidValueGenerator>();
                qrCodeUuid.HasValueGenerator<GuidValueGenerator>();
                fingerprintUuid.HasValueGenerator<GuidValueGenerator>();
                fingerprintDeviceUuid.HasValueGenerator<GuidValueGenerator>();
                fingerprintEnrollmentSessionUuid.HasValueGenerator<GuidValueGenerator>();
                fingerprintScanEventUuid.HasValueGenerator<GuidValueGenerator>();
            }

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                builder.Entity<Session>()
                    .Property(s => s.RowVersion)
                    .IsRequired(false)
                    .ValueGeneratedNever()
                    .IsConcurrencyToken(false);
            }
            else
            {
                builder.Entity<Session>()
                    .Property(s => s.RowVersion)
                    .IsRowVersion();
            }

            // Configure QrCode relationships
            builder.Entity<QrCode>()
                .HasOne(q => q.Session)
                .WithMany(s => s.QrCodes)
                .HasForeignKey(q => q.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure QR hash uniqueness - critical for security and preventing duplicate QR codes
            // This index prevents multiple QR codes from having the same hash value
            builder.Entity<QrCode>()
                .HasIndex(q => q.QrHash)
                .IsUnique()
                .HasDatabaseName("IX_QrCodes_QrHash");

            // Configure optimistic concurrency for QrCode entity
            // Prevents conflicting updates during concurrent QR code scans
            builder.Entity<QrCode>()
                .Property(q => q.RowVersion)
                .IsConcurrencyToken();

            // Configure AttendanceRecord relationships
            builder.Entity<AttendanceRecord>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AttendanceRecord>()
                .HasOne(a => a.Session)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade); // Delete attendance when session is deleted

            builder.Entity<AttendanceRecord>()
                .HasOne(a => a.QrCode)
                .WithMany(q => q.AttendanceRecords)
                .HasForeignKey(a => a.QrCodeId)
                .OnDelete(DeleteBehavior.SetNull); // Set to null if QR code is deleted

            // Composite index for preventing duplicate attendance records
            // Ensures a student can only have one attendance record per session
            builder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.StudentId, a.SessionId })
                .IsUnique()
                .HasDatabaseName("IX_AttendanceRecords_StudentId_SessionId");

            // Configure Fingerprint relationships
            builder.Entity<Fingerprint>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for fast fingerprint lookup by user
            builder.Entity<Fingerprint>()
                .HasIndex(f => f.UserId)
                .HasDatabaseName("IX_Fingerprints_UserId");

            builder.Entity<Fingerprint>()
                .HasIndex(f => f.UserId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_Fingerprints_UserId_Active");

            // Composite index for device + sensor ID lookups
            builder.Entity<Fingerprint>()
                .HasIndex(f => new { f.DeviceId, f.SensorFingerprintId })
                .HasDatabaseName("IX_Fingerprints_DeviceId_SensorFingerprintId");

            builder.Entity<Fingerprint>()
                .HasIndex(f => new { f.DeviceId, f.SensorFingerprintId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_Fingerprints_DeviceId_SensorFingerprintId_Active");

            // Configure FingerprintDevice unique identifier and operational indexes
            builder.Entity<FingerprintDevice>()
                .HasIndex(d => d.DeviceIdentifier)
                .IsUnique()
                .HasDatabaseName("IX_FingerprintDevices_DeviceIdentifier");

            builder.Entity<FingerprintDevice>()
                .HasIndex(d => d.IsActive)
                .HasDatabaseName("IX_FingerprintDevices_IsActive");

            // Configure FingerprintScanEvent relationships
            builder.Entity<FingerprintScanEvent>()
                .HasOne(e => e.Device)
                .WithMany(d => d.ScanEvents)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FingerprintScanEvent>()
                .HasOne(e => e.MatchedStudent)
                .WithMany()
                .HasForeignKey(e => e.MatchedStudentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<FingerprintScanEvent>()
                .HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FingerprintScanEvent>()
                .HasOne(e => e.AttendanceRecord)
                .WithMany()
                .HasForeignKey(e => e.AttendanceRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<FingerprintScanEvent>()
                .HasIndex(e => e.EventId)
                .IsUnique()
                .HasDatabaseName("IX_FingerprintScanEvents_EventId");

            builder.Entity<FingerprintScanEvent>()
                .HasIndex(e => new { e.DeviceId, e.CapturedAt })
                .HasDatabaseName("IX_FingerprintScanEvents_DeviceId_CapturedAt");

            builder.Entity<FingerprintScanEvent>()
                .HasIndex(e => new { e.MatchedStudentId, e.CapturedAt })
                .HasDatabaseName("IX_FingerprintScanEvents_MatchedStudentId_CapturedAt");

            builder.Entity<FingerprintScanEvent>()
                .HasIndex(e => e.Status)
                .HasDatabaseName("IX_FingerprintScanEvents_Status");

            builder.Entity<FingerprintScanEvent>()
                .HasIndex(e => e.AttendanceRecordId)
                .IsUnique()
                .HasFilter("[AttendanceRecordId] IS NOT NULL")
                .HasDatabaseName("IX_FingerprintScanEvents_AttendanceRecordId");

            builder.Entity<FingerprintScanEvent>()
                .Property(e => e.RowVersion)
                .IsRowVersion();

            builder.Entity<FingerprintScanEvent>()
                .Property(e => e.MatchScore)
                .HasPrecision(5, 4);

            builder.Entity<FingerprintScanEvent>()
                .Property(e => e.ThresholdUsed)
                .HasPrecision(5, 4);

            // Configure FingerprintEnrollmentSession relationships and indexes
            builder.Entity<FingerprintEnrollmentSession>()
                .HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FingerprintEnrollmentSession>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FingerprintEnrollmentSession>()
                .HasIndex(e => e.EnrollmentSessionId)
                .IsUnique()
                .HasDatabaseName("IX_FingerprintEnrollmentSessions_EnrollmentSessionId");

            builder.Entity<FingerprintEnrollmentSession>()
                .HasIndex(e => new { e.DeviceId, e.Status })
                .HasDatabaseName("IX_FingerprintEnrollmentSessions_DeviceId_Status");

            builder.Entity<FingerprintEnrollmentSession>()
                .HasIndex(e => new { e.StudentId, e.Status })
                .HasDatabaseName("IX_FingerprintEnrollmentSessions_StudentId_Status");
        }
    }
}
