using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository
{
    /// <summary>
    /// Represents the repository for managing sections.
    /// </summary>
    public interface ISectionRepository
    {
        /// <summary>
        /// Retrieves a section by its ID.
        /// </summary>
        /// <param name="sectionId">The section ID.</param>
        /// <returns>The section if found; otherwise, null.</returns>
        Task<Section?> GetSectionByIdAsync(int sectionId);

        /// <summary>
        /// Retrieves all sections.
        /// </summary>
        /// <returns>A collection of all sections.</returns>
        Task<IEnumerable<Section>> GetAllSectionsAsync();
        
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
        
        /// <summary>
        /// Creates a new section.
        /// </summary>
        /// <param name="section">The section to create.</param>
        /// <returns>The created section.</returns>
        Task<Section> CreateSectionAsync(Section section);

        /// <summary>
        /// Updates an existing section.
        /// </summary>
        /// <param name="id">The section ID.</param>
        /// <param name="section">The section to update.</param>
        /// <returns>The updated section if found; otherwise, null.</returns>
        Task<Section?> UpdateSectionAsync(int id, Section section);

        /// <summary>
        /// Deletes a section by its ID.
        /// </summary>
        /// <param name="id">The section ID.</param>
        /// <returns>True if the section was deleted; otherwise, false.</returns>
        Task<bool> DeleteSectionAsync(int id);
    }
}