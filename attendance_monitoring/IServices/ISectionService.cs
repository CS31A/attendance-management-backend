using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices
{
    public interface ISectionService
    {
        Task<Section> GetSectionByIdAsync(int sectionId);
        Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync();

        /// <summary>
        /// Retrieves all active students in a specific section by section ID.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>A collection of active students in the specified section.</returns>
        Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(int sectionId);

        /// <summary>
        /// Retrieves all students in a specific section by section ID.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>A collection of all students in the specified section.</returns>
        Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(int sectionId);

        Task<SectionResponseDto> CreateSectionAsync(Section section);
        Task<SectionResponseDto> UpdateSectionAsync(int id, Section section);
        Task DeleteSectionAsync(int id);
        Task<bool> HasStudentsInSectionAsync(int sectionId);
        Task<bool> HasStudentEnrollmentsInSectionAsync(int sectionId);
        Task<bool> HasSchedulesInSectionAsync(int sectionId);
    }
}