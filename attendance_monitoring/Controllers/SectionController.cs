using attendance_monitoring.Classes;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SectionController : ControllerBase
    {
        private readonly ISectionService _sectionService;

        public SectionController(ISectionService sectionService)
        {
            _sectionService = sectionService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SectionResponseDto>> GetSection(int id)
        {
            var section = await _sectionService.GetSectionByIdAsync(id);
            
            if (section == null)
            {
                return NotFound($"Section with ID {id} not found.");
            }

            var sectionDto = new SectionResponseDto
            {
                Id = section.Id,
                Name = section.Name,
                InstructorId = section.InstructorId,
                CourseId = section.CourseId,
                CreatedAt = section.CreatedAt,
                UpdatedAt = section.UpdatedAt
            };

            return Ok(sectionDto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SectionResponseDto>>> GetAllSections()
        {
            var sections = await _sectionService.GetAllSectionsAsync();
            return Ok(sections);
        }

        [HttpPost]
        public async Task<ActionResult<SectionResponseDto>> CreateSection([FromBody] CreateSection createSection)
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

            var createdSection = await _sectionService.CreateSectionAsync(section);
            
            if (createdSection == null)
            {
                return BadRequest("Unable to create section.");
            }

            return CreatedAtAction(nameof(GetSection), new { id = createdSection.Id }, createdSection);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SectionResponseDto>> UpdateSection(int id, [FromBody] CreateSection updateSection)
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

            var updatedSection = await _sectionService.UpdateSectionAsync(id, section);
            
            if (updatedSection == null)
            {
                return NotFound($"Section with ID {id} not found.");
            }

            return Ok(updatedSection);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSection(int id)
        {
            var result = await _sectionService.DeleteSectionAsync(id);
            
            if (!result)
            {
                return NotFound($"Section with ID {id} not found.");
            }

            return NoContent();
        }
    }
}