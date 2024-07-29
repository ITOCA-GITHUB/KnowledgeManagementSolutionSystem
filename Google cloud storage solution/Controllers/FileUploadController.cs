using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Google_cloud_storage_solution.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly GoogleDriveService _googleDriveService;
        private readonly HashSet<string> _cloudStorageFileTypes;
        private readonly HashSet<string> _googleDriveFileTypes;
        private readonly GoogleStorageDbContext _dbContext;

        public FileUploadController(ICloudStorageService cloudStorageService, GoogleDriveService googleDriveService, GoogleStorageDbContext DbContext)
        {
            _cloudStorageService = cloudStorageService;
            _googleDriveService = googleDriveService;
            _cloudStorageFileTypes = new HashSet<string> { ".mp4", ".mpeg", ".mkv", ".wmv", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
            _googleDriveFileTypes = new HashSet<string> { ".doc", ".docx", ".pdf", ".xlsx", ".pptx", ".txt" };
            _dbContext = DbContext;
        }

        [HttpGet]
        public IActionResult UploadToGoogleDrive()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadToGoogleDrive(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (_googleDriveFileTypes.Contains(fileExtension))
                {
                    // Upload to Google Drive
                    using (var stream = file.OpenReadStream())
                    {
                        var fileId = await _googleDriveService.UploadFileAsync(stream, file.FileName, file.ContentType);
                        ViewBag.GoogleDriveLink = _googleDriveService.GetFileUrl(fileId);
                    }
                    ViewBag.Message = "File uploaded successfully to Google Drive!";
                }
                else
                {
                    ViewBag.Message = "Unsupported file type.";
                }
            }
            else
            {
                ViewBag.Message = "Please select a file to upload.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult UploadToGoogleCLoud()
        {
            return View();
        }
        public async Task<IActionResult> UploadToGoogleCLoud(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (_cloudStorageFileTypes.Contains(fileExtension))
                {
                    // Upload to Google Cloud Storage
                    using (var stream = file.OpenReadStream())
                    {
                        await _cloudStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);
                    }
                    ViewBag.Message = "File uploaded successfully to Google Cloud Storage!";
                }
                else
                {
                    ViewBag.Message = "Unsupported file type.";
                }
            }
            else
            {
                ViewBag.Message = "Please select a file to upload.";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePermissions(string objectName)
        {
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var email = await _dbContext.GetEmailByUsernameAsync(username);
                if (!string.IsNullOrEmpty(email))
                {
                    await _cloudStorageService.UpdateFilePermissionsAsync(objectName, email);
                }
            }
            return RedirectToAction("Index");
        }
    }
}
