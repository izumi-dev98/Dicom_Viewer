using Dicom_Viewer.Models;
using Dicom_Viewer.Repositories;
using Dicom_Viewer.Services;
using ExpressionExtensionSQL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dicom_Viewer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DicomController : ControllerBase
    {
        private readonly DicomViewerRepo _repository;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public DicomController(DicomViewerRepo repository, IWebHostEnvironment env, IConfiguration configuration)
        {
            _repository = repository;
            _env = env;
            _configuration = configuration;
        }
        // Get all studies from the database

        [HttpGet("GetAllStudies")]
        public async Task<IActionResult> GetAllStudies()
        {
            var studies = await _repository.GelAllStudies();

            if(studies == null || !studies.Any())
            {
                return NotFound("No studies found.");
            }
            return Ok(studies);
        }

        // Get a study by its StudyInstanceUID

        [HttpGet("GetStudyByUID/{studyInstanceUID}")]
        public async Task<IActionResult> GetStudyByUID(string studyInstanceUID)
        {
            var study = await _repository.GetStudyByUID(studyInstanceUID);
            if (study == null)
            {
                return NotFound("Study not found.");
            }
            return Ok(study);
        }

        // Post a new study to the database

        [HttpPost("PostStudy")]
        public async Task<IActionResult> PostStudy(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File Not Provided");

            var storageFolder = Path.Combine(_env.ContentRootPath, "Storage", "dicom_files");
            if (!Directory.Exists(storageFolder)) Directory.CreateDirectory(storageFolder);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(storageFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

           DicomHapper.ProcessAndSave   (filePath, _configuration);

            return Ok(new { Message = "Dicom File Save Successfully" });
        }

        // Delete a study from the database by its StudyInstanceUID

        [HttpDelete("DeleteStudy/{studyInstanceUID}")]
        public async Task<IActionResult> DeleteStudy(string studyInstanceUID)
        {
            var result = await _repository.DeleteStudy(studyInstanceUID);
            if (result == 0)
            {
                return NotFound("Study not found.");
            }
            return Ok(new { Message = "Study deleted successfully." });
        }

    }
}
