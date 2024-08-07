using CsvHelper;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Models;
using Google_cloud_storage_solution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using OfficeOpenXml;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Google_cloud_storage_solution.Controllers
{
    public class StorageController : Controller
    {
        public int ActivityId { get; set; }
        private readonly StorageClient _storageClient;
        private readonly GoogleCloudStorageService _cloudStorageService;
        private readonly GoogleStorageDbContext _dbContext;
        private readonly string _bucketName = "kms_cloud_storage";

        public StorageController(GoogleCloudStorageService cloudStorageService, GoogleStorageDbContext dbContext)
        {
            var credential = GoogleCredential.FromFile("GoogleStorageBucket.json");
            _cloudStorageService = cloudStorageService;
            _dbContext = dbContext;
            _storageClient = StorageClient.Create(credential);

        }

        public async Task<IActionResult> Index(string prefix = "")
        {
            TimeTracking(nameof(Index));
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

            var signedUrls = storageObjects.ToDictionary(
               obj => obj.Name,
               obj => _cloudStorageService.GenerateSignedUrl(obj.Name, TimeSpan.FromHours(3)) // Adjust duration as needed
             );

            ViewBag.Prefix = prefix;
            ViewBag.SignedUrls = signedUrls;
            return View(storageObjects);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder(string parentFolderPath, string newFolderName)
        {
            if (string.IsNullOrEmpty(parentFolderPath) || string.IsNullOrEmpty(newFolderName))
            {
                // Handle invalid input
                return BadRequest("Parent folder path and new folder name are required.");
            }

            var newFolderPath = $"{parentFolderPath.TrimEnd('/')}/{newFolderName.TrimStart('/')}";

            // Add a trailing slash to ensure it's recognized as a folder
            if (!newFolderPath.EndsWith("/"))
            {
                newFolderPath += "/";
            }

            try
            {
                // Create an empty object with the new folder path to create the folder
                var bucketName = "kms_cloud_storage";
                var newFolderObject = new StorageObject
                {
                    Name = newFolderPath
                };

                await _cloudStorageService.UploadObjectAsync(bucketName, newFolderPath, null); // Ensure this method handles creating empty folders
                return RedirectToAction("Index", new { prefix = parentFolderPath });
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadFile()
        {
            // Retrieve the current user's email
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User email is required.");
            }

            var folders = await GetFoldersAsync(userEmail);
            ViewBag.Folders = folders;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string folderPath)
        {
            if (file == null || file.Length == 0 || string.IsNullOrEmpty(folderPath))
            {
                return BadRequest("File and folder path are required.");
            }

            var filePath = $"{folderPath.TrimEnd('/')}/{file.FileName}";

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    await _storageClient.UploadObjectAsync(_bucketName, filePath, null, stream);
                }

                var signedUrl = GenerateSignedUrl(filePath);

                ViewBag.SignedUrl = signedUrl;
                return View("UploadSuccess"); // Create a view to display the success message and signed URL
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GenerateSignedUrl(string objectName, int expirationMinutes = 60)
        {
            var credential = GoogleCredential.FromFile("GoogleStorageBucket.json");
            UrlSigner urlSigner = UrlSigner.FromServiceAccountCredential(credential.UnderlyingCredential as ServiceAccountCredential);

            var url = urlSigner.Sign(
                _bucketName,
                objectName,
                TimeSpan.FromMinutes(expirationMinutes),
                HttpMethod.Get);

            return url;
        }

        private void TimeTracking(string pageName)
        {
            var userName = HttpContext.User.Identity.Name;
            var currentTime = DateTime.Now;

            var userActivity = new UserActivity
            {
                UserName = userName,
                PageName = pageName,
                EntryTime = currentTime.TimeOfDay,
            };

            _dbContext.UserActivities.Add(userActivity);
            _dbContext.SaveChanges();


            ExportActivityDetailsToCsv(userActivity);
            ExportActivityDetailsToExcel(userActivity);

            // Pass the activity ID to the view so it can be used for recording the exit time
            ViewBag.ActivityId = userActivity.Id;
        }

        private void ExportActivityDetailsToCsv(UserActivity activity)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "login_details.csv");
            var activityDetails = new
            {
                UserName = activity.UserName,
                PageName = activity.PageName,
                EntryTime = activity.EntryTime.ToString(@"hh\:mm\:ss"),
                ExitTime = activity.ExitTime.ToString(@"hh\:mm\:ss")
            };

            using (var writer = new StreamWriter(filePath, append: true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecord(activityDetails);
                writer.WriteLine(); // Ensures each record is on a new line
            }
        }

        private void ExportActivityDetailsToExcel(UserActivity activity)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "login_details.xlsx");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets.Add("ActivityDetails");
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                worksheet.Cells[rowCount + 1, 1].Value = activity.UserName;
                worksheet.Cells[rowCount + 1, 2].Value = activity.PageName;
                worksheet.Cells[rowCount + 1, 3].Value = activity.EntryTime.ToString(@"hh\:mm\:ss");
                worksheet.Cells[rowCount + 1, 4].Value = activity.ExitTime.ToString(@"hh\:mm\:ss");

                package.Save();
            }
        }
        private async Task<List<string>> GetFoldersAsync(string userEmail)
        {
            var folders = new List<string>();

            // Fetch all objects in the bucket
            var objects = _storageClient.ListObjects(_bucketName, "");

            // Normalize the user email for comparison
            var normalizedEmail = userEmail.Trim().ToLower();
            Console.WriteLine($"Normalized User Email: {normalizedEmail}"); // Debugging print

            foreach (var obj in objects)
            {
                // Ensure that the object is part of a folder and not a file
                var parts = obj.Name.Split('/');
                Console.WriteLine($"Object Name: {obj.Name}"); // Debugging print

                if (parts.Length > 1)
                {
                    // Extract the folder name
                    var folder = string.Join("/", parts.Take(parts.Length - 1));
                    Console.WriteLine($"Extracted Folder: {folder}"); // Debugging print

                    // Check if the folder matches the user's email
                    if (parts.Length > 2 && parts[1].Trim().ToLower() == normalizedEmail)
                    {
                        // Add the folder to the list if not already included
                        if (!folders.Contains(folder))
                        {
                            folders.Add(folder);
                        }
                    }
                }
            }
            return folders;
        }
    }
}
