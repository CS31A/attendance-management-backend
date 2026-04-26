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
    public class SectionController(ISectionService sectionService, ICourseService courseService, ILogger<SectionController> logger) : ControllerBase
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
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<SectionResponseDto>> GetSection(Guid id)
        {
            try
            {
                var section = await sectionService.GetSectionByIdAsync(id);
                return Ok(MapSectionResponse(section));
            }
            catch (EntityNotFoundException<Guid> ex)
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

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SectionResponseDto>> GetSectionByUuid([FromRoute(Name = "id")] Guid id)
        {
            try
            {
                var section = await sectionService.GetSectionByUuidAsync(id);
                return Ok(MapSectionResponse(section));
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Section with UUID {SectionId} not found", id);
                return NotFound($"Section with UUID {id} not found");
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while retrieving section with UUID {SectionId}", id);
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

                var courseId = await ResolveCourseIdAsync(createSection).ConfigureAwait(false);

                var section = new Section
                {
                    Name = createSection.Name,
                    CourseId = courseId
                };

                var createdSection = await sectionService.CreateSectionAsync(section);
                return CreatedAtAction(nameof(GetSectionByUuid), new { id = createdSection.Id }, createdSection);
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while creating section with name {SectionName}", createSection.Name);
                return BadRequest(ex.Message);
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Referenced course ID {CourseId} was not found while creating section", createSection.CourseId);
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                logger.LogWarning(ex, "Invalid section create request for section {SectionName}", createSection.Name);
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPut("{id:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<SectionResponseDto>> UpdateSection(Guid id, [FromBody] CreateSection updateSection)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var courseId = await ResolveCourseIdAsync(updateSection).ConfigureAwait(false);

                var section = new Section
                {
                    Name = updateSection.Name,
                    CourseId = courseId
                };

                var updatedSection = await sectionService.UpdateSectionAsync(id, section);
                return Ok(updatedSection);
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Section with ID {SectionId} not found for update", id);
                return NotFound($"Section with ID {id} not found");
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while updating section with ID {SectionId}", id);
                return BadRequest(ex.Message);
            }
            catch (ValidationException ex)
            {
                logger.LogWarning(ex, "Invalid section update request for section ID {SectionId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<SectionResponseDto>> UpdateSectionByUuid([FromRoute(Name = "id")] Guid id, [FromBody] CreateSection updateSection)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var courseId = await ResolveCourseIdAsync(updateSection).ConfigureAwait(false);

                var section = new Section
                {
                    Name = updateSection.Name,
                    CourseId = courseId
                };

                var updatedSection = await sectionService.UpdateSectionByUuidAsync(id, section);
                return Ok(updatedSection);
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Section or course UUID not found while updating section UUID {SectionId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while updating section with UUID {SectionId}", id);
                return BadRequest(ex.Message);
            }
            catch (ValidationException ex)
            {
                logger.LogWarning(ex, "Invalid section update request for section UUID {SectionId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpDelete("{id:int}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult> DeleteSection(Guid id)
        {
            try
            {
                await sectionService.DeleteSectionAsync(id);
                return NoContent();
            }
            catch (EntityNotFoundException<Guid> ex)
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

        [Authorize(Policy = "AdminPolicy")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteSectionByUuid([FromRoute(Name = "id")] Guid id)
        {
            try
            {
                await sectionService.DeleteSectionByUuidAsync(id);
                return NoContent();
            }
            catch (EntityNotFoundException<Guid> ex)
            {
                logger.LogWarning(ex, "Section with UUID {SectionId} not found for deletion", id);
                return NotFound(new { message = ex.Message });
            }
            catch (EntityConflictException ex)
            {
                logger.LogWarning(ex, "Cannot delete section UUID {SectionId}: {ConflictReason}", id, ex.Message);
                return Conflict(CreateErrorResponse(ex.Message, StatusCodes.Status409Conflict));
            }
            catch (EntityServiceException ex)
            {
                logger.LogError(ex, "Service error occurred while deleting section with UUID {SectionId}", id);
                return StatusCode(500, "An error occurred while deleting the section");
            }
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpGet("{sectionId:int}/active-students")]
        public async Task<ActionResult<IEnumerable<Student>>> GetActiveStudentsBySectionId(Guid sectionId)
        {
            try
            {
                if (sectionId == Guid.Empty)
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
        public async Task<ActionResult<IEnumerable<Student>>> GetAllStudentsBySectionId(Guid sectionId)
        {
            try
            {
                if (sectionId == Guid.Empty)
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
        public async Task<ActionResult<bool>> HasStudentsInSection(Guid sectionId)
        {
            try
            {
                if (sectionId == Guid.Empty)
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
        public async Task<ActionResult<bool>> HasStudentEnrollmentsInSection(Guid sectionId)
        {
            try
            {
                if (sectionId == Guid.Empty)
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
        public async Task<ActionResult<bool>> HasSchedulesInSection(Guid sectionId)
        {
            try
            {
                if (sectionId == Guid.Empty)
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

        private async Task<Guid> ResolveCourseIdAsync(CreateSection request)
        {
            if (!request.CourseId.HasValue || request.CourseId.Value == Guid.Empty)
            {
                throw new ValidationException("Course reference is required.");
            }

            var course = await courseService.GetCourseByUuidAsync(request.CourseId.Value).ConfigureAwait(false);
            if (course == null)
            {
                throw new EntityNotFoundException<Guid>("Course", request.CourseId.Value);
            }

            return course.Id;
        }

        private static SectionResponseDto MapSectionResponse(Section section)
        {
            return new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                CourseId = section.Course?.Id,
                CreatedAt = section.CreatedAt,
                UpdatedAt = section.UpdatedAt
            };
        }
    }
}
