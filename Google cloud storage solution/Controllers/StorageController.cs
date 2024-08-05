using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Google_cloud_storage_solution.Controllers
{
    public class StorageController : Controller
    {
        private readonly GoogleCloudStorageService _cloudStorageService;
        private readonly GoogleStorageDbContext _dbContext;

        public StorageController(GoogleCloudStorageService cloudStorageService, GoogleStorageDbContext dbContext)
        {
            _cloudStorageService = cloudStorageService;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(string prefix = "")
        {
            var bucketName = "kms_cloud_storage";
            var storageObjects = await _cloudStorageService.ListObjectsAsync(bucketName, prefix);

            // Retrieve the current user's email
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (!string.IsNullOrEmpty(userEmail))
            {
                // Check if the user is from the Executive department
                var user = _dbContext.Users.FirstOrDefault(u => u.Email == userEmail);
                if (user != null && user.Department == "Executive")
                {
                    // If the user is from the Executive department, show all folders
                    // No additional filtering is needed
                }
                else
                {
                    // Normalize the email and filter storage objects to only show folders that match the user's email
                    var normalizedEmail = userEmail.Trim().ToLower();
                    storageObjects = storageObjects
                        .Where(obj => obj.Name.Split('/').Any(part => part.Trim().ToLower() == normalizedEmail))
                        .ToList();
                }
            }

            ViewBag.Prefix = prefix;
            return View(storageObjects);
        }
    }
}
