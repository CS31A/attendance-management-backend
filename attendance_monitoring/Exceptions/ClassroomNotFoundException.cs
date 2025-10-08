using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Exception thrown when a classroom is not found
    /// </summary>
    public class ClassroomNotFoundException : Exception
    {
        public int ClassroomId { get; }

        public ClassroomNotFoundException(int classroomId) : base($"Classroom with ID {classroomId} was not found.")
        {
            ClassroomId = classroomId;
        }

        public ClassroomNotFoundException(int classroomId, string message) : base(message)
        {
            ClassroomId = classroomId;
        }
    }
}