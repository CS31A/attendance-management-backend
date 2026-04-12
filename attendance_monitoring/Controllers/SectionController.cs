using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers
{
    [Authorize(Policy = "UserPolicy")]
    [ApiController]
    [Route("api/sections")]
    public class SectionController(ISectionService sectionService, ILogger<SectionController> logger) : ControllerBase
    {
        private ErrorResponseDto CreateErrorResponse(string message, int statusCode)
        {
            return new ErrorResponseDto
            {
                Success = false,
                Message = message,
                StatusCode = statusCode,
                Path = Request?.Path,
                Timestamp = DateTime.UtcNow
            };
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SectionResponseDto>> GetSection(int id)
        {
            try
            {
                var section = await sectionService.GetSectionByIdAsync(id);
                return Ok(new SectionResponseDto
                {
                    Id = section.Id,
                    Name = section.Name,
                    CourseId = section.CourseId,
                    CreatedAt = section.CreatedAt,
                    UpdatedAt = section.UpdatedAt
                });
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Section with ID {SectionId} not found", id);
                return NotFound($"Section with ID {id} not found");
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while retrieving section with ID {SectionId}", id);
                return StatusCode(500, "An error occurred while retrieving the section");
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
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while retrieving all sections");
                return StatusCode(500, "An error occurred while retrieving sections");
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        public async Task<ActionResult<SectionResponseDto>> CreateSection([FromBody] CreateSection createSection)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var section = new Section
                {
                    Name = createSection.Name,
                    CourseId = createSection.CourseId
                };

                var createdSection = await sectionService.CreateSectionAsync(section);
                return CreatedAtAction(nameof(GetSection), new { id = createdSection.Id }, createdSection);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while creating section with name {SectionName}", createSection.Name);
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<SectionResponseDto>> UpdateSection(int id, [FromBody] CreateSection updateSection)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var section = new Section
                {
                    Name = updateSection.Name,
                    CourseId = updateSection.CourseId
                };

                var updatedSection = await sectionService.UpdateSectionAsync(id, section);
                return Ok(updatedSection);
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Section with ID {SectionId} not found for update", id);
                return NotFound($"Section with ID {id} not found");
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while updating section with ID {SectionId}", id);
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteSection(int id)
        {
            try
            {
                await sectionService.DeleteSectionAsync(id);
                return NoContent();
            }
            catch (EntityNotFoundException<int> ex)
            {
                logger.LogWarning(ex, "Section with ID {SectionId} not found for deletion", id);
                return NotFound($"Section with ID {id} not found");
            }
            catch (EntityConflictException ex)
            {
                logger.LogWarning(ex, "Cannot delete section {SectionId}: {ConflictReason}", id, ex.Message);
                return Conflict(CreateErrorResponse(ex.Message, StatusCodes.Status409Conflict));
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while deleting section with ID {SectionId}", id);
                return StatusCode(500, "An error occurred while deleting the section");
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
                return Ok(students);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while retrieving active students for section with ID {SectionId}", sectionId);
                return StatusCode(500, "An error occurred while retrieving active students");
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
                return Ok(students);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while retrieving all students for section with ID {SectionId}", sectionId);
                return StatusCode(500, "An error occurred while retrieving students");
            }
        }

        [Authorize(Policy = "PrivilegedPolicy")]
        [HttpGet("{sectionId:int}/has-students")]
        public async Task<ActionResult<bool>> HasStudentsInSection(int sectionId)
        {
            try
            {
                if (sectionId <= 0)
                {
                    logger.LogWarning("Invalid section ID {SectionId} provided for dependency check.", sectionId);
                    return BadRequest("Section ID must be greater than 0.");
                }

                var hasStudents = await sectionService.HasStudentsInSectionAsync(sectionId);
                return Ok(hasStudents);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while checking students for section with ID {SectionId}", sectionId);
                return StatusCode(500, "An error occurred while checking section dependencies");
            }
        }

        [Authorize(Policy = "PrivilegedPolicy")]
        [HttpGet("{sectionId:int}/has-enrollments")]
        public async Task<ActionResult<bool>> HasStudentEnrollmentsInSection(int sectionId)
        {
            try
            {
                if (sectionId <= 0)
                {
                    logger.LogWarning("Invalid section ID {SectionId} provided for dependency check.", sectionId);
                    return BadRequest("Section ID must be greater than 0.");
                }

                var hasEnrollments = await sectionService.HasStudentEnrollmentsInSectionAsync(sectionId);
                return Ok(hasEnrollments);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while checking enrollments for section with ID {SectionId}", sectionId);
                return StatusCode(500, "An error occurred while checking section dependencies");
            }
        }

        [Authorize(Policy = "PrivilegedPolicy")]
        [HttpGet("{sectionId:int}/has-schedules")]
        public async Task<ActionResult<bool>> HasSchedulesInSection(int sectionId)
        {
            try
            {
                if (sectionId <= 0)
                {
                    logger.LogWarning("Invalid section ID {SectionId} provided for dependency check.", sectionId);
                    return BadRequest("Section ID must be greater than 0.");
                }

                var hasSchedules = await sectionService.HasSchedulesInSectionAsync(sectionId);
                return Ok(hasSchedules);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while checking schedules for section with ID {SectionId}", sectionId);
                return StatusCode(500, "An error occurred while checking section dependencies");
            }
        }
    }
}
