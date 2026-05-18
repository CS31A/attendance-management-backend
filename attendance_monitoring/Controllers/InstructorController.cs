using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[Authorize]
[ApiController]
[Route("api/instructors")]
public class InstructorController(IInstructorCrudService crudService, IInstructorQueryService queryService, IInstructorDetailService detailService, ILogger<InstructorController> logger) : ControllerBase
{
    #region Read Operations

    // GET: api/Instructor
    [HttpGet]
    public async Task<ActionResult<IList<Instructor>>> GetInstructors()
    {
        logger.LogInformation("Getting all instructors");
        var instructors = await crudService.GetAllInstructorsAsync();
        logger.LogInformation("Successfully retrieved {Count} instructors", instructors.Count);
        return Ok(instructors);
    }

    // GET: api/Instructor/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Instructor>> GetInstructor(Guid id)
    {
        logger.LogInformation("Getting instructor with ID: {Id}", id);
        var instructor = await crudService.GetInstructorByIdAsync(id);
        logger.LogInformation("Successfully retrieved instructor with ID: {Id}", id);
        return Ok(instructor);
    }

    // GET: api/instructors/{instructorId}/subjects
    [HttpGet("{instructorId:int}/subjects")]
    public async Task<ActionResult<IEnumerable<SubjectResponseDto>>> GetInstructorSubjects(Guid instructorId)
    {
        logger.LogInformation("Getting subjects for instructor ID: {InstructorId}", instructorId);
        var subjects = await queryService.GetSubjectsByInstructorIdAsync(instructorId);
        logger.LogInformation("Successfully retrieved subjects for instructor ID: {InstructorId}", instructorId);
        return Ok(subjects);
    }

    // GET: api/instructors/profile
    [HttpGet("profile")]
    public async Task<ActionResult<InstructorProfileResponseDto>> GetInstructorProfile()
    {
        logger.LogInformation("Getting instructor profile for authenticated user");
        var profile = await queryService.GetInstructorProfileAsync(User);

        if (profile == null)
        {
            logger.LogWarning("No instructor profile found for authenticated user");
            return NotFound("No instructor profile found for the current user");
        }

        logger.LogInformation("Successfully retrieved instructor profile with ID: {InstructorId}", profile.Id);
        return Ok(profile);
    }

    // GET: api/instructors/me/schedules
    [HttpGet("me/schedules")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetMySchedules()
    {
        logger.LogInformation("Getting schedules for authenticated instructor");
        var schedules = await queryService.GetSchedulesByInstructorAsync(User);
        logger.LogInformation("Successfully retrieved {Count} schedules for authenticated instructor", schedules.Count());
        return Ok(schedules);
    }

    // GET: api/instructors/me/sections-with-students
    [HttpGet("me/sections-with-students")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<InstructorSectionsWithStudentsResponseDto>> GetMySectionsWithStudents()
    {
        logger.LogInformation("Getting sections with students for authenticated instructor");
        var response = await queryService.GetSectionsWithStudentsByInstructorAsync(User);
        logger.LogInformation("Successfully retrieved sections with students for instructor ID: {InstructorId}", response.InstructorId);
        return Ok(response);
    }

    // GET: api/instructors/me/sections
    [HttpGet("me/sections")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<List<InstructorSectionOverviewDto>>> GetMySectionsOverview()
    {
        logger.LogInformation("Getting sections overview for authenticated instructor");
        var sections = await detailService.GetInstructorSectionsOverviewAsync(User);
        logger.LogInformation("Successfully retrieved {Count} section overviews for authenticated instructor", sections.Count);
        return Ok(sections);
    }

    // GET: api/instructors/me/sections/{id:guid}
    [HttpGet("me/sections/{id:guid}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<InstructorSectionDetailDto>> GetMySectionDetail([FromRoute(Name = "id")] Guid sectionId)
    {
        logger.LogInformation("Getting section detail for section UUID: {SectionId}", sectionId);
        var sectionDetail = await detailService.GetInstructorSectionDetailByUuidAsync(User, sectionId);
        logger.LogInformation("Successfully retrieved section detail for section UUID: {SectionId}", sectionId);
        return Ok(sectionDetail);
    }

    // GET: api/instructors/me/students/{id:guid}
    [HttpGet("me/students/{id:guid}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<InstructorStudentDetailDto>> GetMyStudentDetail([FromRoute(Name = "id")] Guid studentId)
    {
        logger.LogInformation("Getting student detail for student UUID: {StudentId}", studentId);
        var studentDetail = await detailService.GetInstructorStudentDetailByUuidAsync(User, studentId);
        logger.LogInformation("Successfully retrieved student detail for student UUID: {StudentId}", studentId);
        return Ok(studentDetail);
    }

    #endregion

    #region Update Operations

    // PATCH: api/Instructor/{id}
    [HttpPatch("{id:int}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<Instructor>> PatchInstructor(Guid id, UpdateInstructor updateInstructor)
    {
        logger.LogInformation("Updating instructor with ID: {Id}", id);
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Instructor update failed due to invalid model state for instructor ID: {Id}", id);
            return BadRequest(ModelState);
        }

        var instructor = await crudService.UpdateInstructorAsync(id, updateInstructor, User);
        logger.LogInformation("Successfully updated instructor with ID: {Id}", id);
        return Ok(instructor);
    }

    #endregion

    #region Delete Operations

    // PATCH: api/Instructor/{id}/soft-delete
    [HttpPatch("{id:int}/soft-delete")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> SoftDeleteInstructor(Guid id)
    {
        logger.LogInformation("Soft deleting instructor with ID: {Id}", id);
        await crudService.SoftDeleteInstructorAsync(id, User);
        logger.LogInformation("Soft delete operation completed for instructor with ID: {Id}", id);
        return Ok(new SoftDeleteResponse
        {
            Success = true,
            Message = "Instructor marked as deleted successfully"
        });
    }

    // DELETE: api/Instructor/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> HardDeleteInstructor(Guid id)
    {
        logger.LogInformation("Hard deleting instructor with ID: {Id}", id);
        await crudService.HardDeleteInstructorAsync(id, User);
        logger.LogInformation("Hard delete operation completed for instructor with ID: {Id}", id);
        return Ok(new SoftDeleteResponse
        {
            Success = true,
            Message = "Instructor permanently deleted successfully"
        });
    }

    // PATCH: api/Instructor/{id}/restore
    [HttpPatch("{id:int}/restore")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<ActionResult<SoftDeleteResponse>> RestoreInstructor(Guid id)
    {
        logger.LogInformation("Restoring instructor with ID: {Id}", id);
        await crudService.RestoreInstructorAsync(id, User);
        logger.LogInformation("Restore operation completed for instructor with ID: {Id}", id);
        return Ok(new SoftDeleteResponse
        {
            Success = true,
            Message = "Instructor restored successfully"
        });
    }

    #endregion
}
