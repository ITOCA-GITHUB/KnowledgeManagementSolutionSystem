using Google_cloud_storage_solution.Services;
using Microsoft.AspNetCore.Mvc;

namespace Google_cloud_storage_solution.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly GoogleDriveService _googleDriveService;

        public FileUploadController(ICloudStorageService cloudStorageService, GoogleDriveService googleDriveService)
        {
            _cloudStorageService = cloudStorageService;
            _googleDriveService = googleDriveService;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                // Upload to Google Cloud Storage
                using (var stream = file.OpenReadStream())
                {
                    await _cloudStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);
                }

                // Upload to Google Drive
                using (var stream = file.OpenReadStream())
                {
                    var fileId = await _googleDriveService.UploadFileAsync(stream, file.FileName, file.ContentType);
                    ViewBag.GoogleDriveLink = _googleDriveService.GetFileUrl(fileId);
                }

                ViewBag.Message = "File uploaded successfully!";
            }
            else
            {
                ViewBag.Message = "Please select a file to upload.";
            }

            return View();
        }
    }
}
