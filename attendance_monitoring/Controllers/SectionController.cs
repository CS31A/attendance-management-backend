using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    [ApiController]
    [Route("api/sections")]
    public class SectionController(ISectionService sectionService, ILogger<SectionController> logger) : ControllerBase
    {
        [Authorize(Policy = "UserPolicy")]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SectionResponseDto>> GetSection(int id)
        {
            try
            {
                var section = await sectionService.GetSectionByIdAsync(id);

                if (section != null)
                    return Ok(new SectionResponseDto
                    {
                        Id = section.Id,
                        Name = section.Name,
                        InstructorId = section.InstructorId,
                        CourseId = section.CourseId,
                        CreatedAt = section.CreatedAt,
                        UpdatedAt = section.UpdatedAt
                    });
                
                logger.LogWarning("Section with ID {SectionId} not found.", id);
                return NotFound($"Section with ID {id} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving section with ID {SectionId}.", id);
                return StatusCode(500, "An error occurred while retrieving the section.");
            }
        }

        [Authorize(Policy = "PrivilegedPolicy")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SectionResponseDto>>> GetAllSections()
        {
            try
            {
                var sections = await sectionService.GetAllSectionsAsync();
                return Ok(sections);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all sections.");
                return StatusCode(500, "An error occurred while retrieving sections.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<SectionResponseDto>> CreateSection([FromBody] CreateSection createSection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var section = new Section
                {
                    Name = createSection.Name,
                    InstructorId = createSection.InstructorId,
                    CourseId = createSection.CourseId
                };

                var createdSection = await sectionService.CreateSectionAsync(section);

                if (createdSection != null)
                    return CreatedAtAction(nameof(GetSection), new { id = createdSection.Id }, createdSection);
                
                logger.LogWarning("Unable to create section with name {SectionName}.", section.Name);
                return BadRequest("Unable to create section.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating section with name {SectionName}.", createSection.Name);
                return StatusCode(500, "An error occurred while creating the section.");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<SectionResponseDto>> UpdateSection(int id, [FromBody] CreateSection updateSection)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var section = new Section
                {
                    Name = updateSection.Name,
                    InstructorId = updateSection.InstructorId,
                    CourseId = updateSection.CourseId
                };

                var updatedSection = await sectionService.UpdateSectionAsync(id, section);

                if (updatedSection != null) return Ok(updatedSection);
                
                logger.LogWarning("Section with ID {SectionId} not found for update.", id);
                return NotFound($"Section with ID {id} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating section with ID {SectionId}.", id);
                return StatusCode(500, "An error occurred while updating the section.");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteSection(int id)
        {
            try
            {
                var result = await sectionService.DeleteSectionAsync(id);

                if (result) return NoContent();
                
                logger.LogWarning("Section with ID {SectionId} not found for deletion.", id);
                return NotFound($"Section with ID {id} not found.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting section with ID {SectionId}.", id);
                return StatusCode(500, "An error occurred while deleting the section.");
            }
        }
        
        [Authorize(Policy = "UserPolicy")]
        [HttpGet("{sectionId:int}/active-students")]
        public async Task<ActionResult<IEnumerable<Student>>> GetActiveStudentsBySectionId(int sectionId)
        {
            try
            {
                if (sectionId <= 0)
                {
                    logger.LogWarning("Invalid section ID {SectionId} provided for active students retrieval.", sectionId);
                    return BadRequest("Section ID must be greater than 0.");
                }
                
                var students = await sectionService.GetActiveStudentsBySectionIdAsync(sectionId);

                if (students.Any()) return Ok(students);
                
                logger.LogWarning("No active students found for section with ID {SectionId}.", sectionId);
                return NotFound($"No active students found for section with ID {sectionId}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving active students for section with ID {SectionId}.", sectionId);
                return StatusCode(500, "An error occurred while retrieving active students.");
            }
        }
        
        [Authorize(Policy = "UserPolicy")]
        [HttpGet("{sectionId:int}/all-students")]
        public async Task<ActionResult<IEnumerable<Student>>> GetAllStudentsBySectionId(int sectionId)
        {
            try
            {
                if (sectionId <= 0)
                {
                    logger.LogWarning("Invalid section ID {SectionId} provided for all students retrieval.", sectionId);
                    return BadRequest("Section ID must be greater than 0.");
                }
                
                var students = await sectionService.GetAllStudentsBySectionIdAsync(sectionId);

                if (students.Any()) return Ok(students);
                
                logger.LogWarning("No students found for section with ID {SectionId}.", sectionId);
                return NotFound($"No students found for section with ID {sectionId}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all students for section with ID {SectionId}.", sectionId);
                return StatusCode(500, "An error occurred while retrieving students.");
            }
        }
    }
}