using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices
{
    public interface ISectionService
    {
        Task<Section> GetSectionByIdAsync(Guid sectionId);
        Task<Section> GetSectionByUuidAsync(Guid id);
        Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync();

        /// <summary>
        /// Retrieves all active students in a specific section by section ID.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>A collection of active students in the specified section.</returns>
        Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(Guid sectionId);

        /// <summary>
        /// Retrieves all students in a specific section by section ID.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>A collection of all students in the specified section.</returns>
        Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(Guid sectionId);

        Task<SectionResponseDto> CreateSectionAsync(Section section);
        Task<SectionResponseDto> UpdateSectionAsync(Guid id, Section section);
        Task<SectionResponseDto> UpdateSectionByUuidAsync(Guid id, Section section);
        Task DeleteSectionAsync(Guid id);
        Task DeleteSectionByUuidAsync(Guid id);
        Task<bool> HasStudentsInSectionAsync(Guid sectionId);
        Task<bool> HasStudentEnrollmentsInSectionAsync(Guid sectionId);
        Task<bool> HasSchedulesInSectionAsync(Guid sectionId);
    }
}
