using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Mvc;

namespace Google_cloud_storage_solution.Controllers
{
    public class FileAccessController : Controller
    {
        private readonly StorageClient _storageClient;
        private readonly GoogleStorageDbContext _context;

        public FileAccessController(GoogleStorageDbContext context)
        {
            var credential = GoogleCredential.FromFile("GoogleStorageBucket.json");
            _context = context;
            _storageClient = StorageClient.Create(credential);
        }

        [HttpGet]
        public IActionResult EditAccess(string fileName)
        {
            ViewBag.Users = _context.Users.ToList(); // Ensure this line populates ViewBag.Users
            ViewBag.AccessTypes = new List<string> { "Reader", "Owner" };
            var model = new EditAccessViewModel
            {
                FileName = fileName
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditAccess(EditAccessViewModel model)
        {
            ViewBag.Users = _context.Users.ToList();
            ViewBag.AccessTypes = new List<string> { "Reader", "Owner" };

            try
            {
                // Retrieve the user's email from the database
                var user = _context.Users.FirstOrDefault(u => u.UserId == model.UserId);
                if (user == null)
                {
                    ViewBag.Message = "User not found.";
                    return View(model);
                }

                var userEmail = user.Email;

                // Ensure the bucket and object names are correct
                var bucketName = "kms_cloud_storage";
                var fileName = model.FileName;

                // Fetch the object
                var storageObject = await _storageClient.GetObjectAsync(bucketName, fileName);

                // Validate and set the role
                string role = model.AccessType.ToUpper();
                if (role != "OWNER" && role != "READER")
                {
                    ViewBag.Message = "Invalid role specified.";
                    return View(model);
                }

                // Update ACL
                var acl = new List<ObjectAccessControl>
                {
                    new ObjectAccessControl
                    {
                        Entity = $"user-{userEmail}",
                        Role = role
                    }
                };

                await UpdateGoogleCloudPermissions(fileName, acl);

                ViewBag.Message = "Access updated successfully!";
            }
            catch (Google.GoogleApiException e)
            {
                ViewBag.Message = $"Error: {e.Message}";
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult GenerateSignedUrl()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateSignedUrl(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    ViewBag.Message = "File name is required.";
                    return View();
                }

                var bucketName = "kms_cloud_storage";

                // Define the signed URL duration (e.g., 1 hour)
                var duration = TimeSpan.FromHours(1);

                // Create the URL signer using the service account credentials
                UrlSigner urlSigner = UrlSigner.FromServiceAccountPath("GoogleStorageBucket.json");

                // Generate the signed URL
                string signedUrl = urlSigner.Sign(bucketName, fileName, duration, HttpMethod.Get);

                if (string.IsNullOrEmpty(signedUrl))
                {
                    ViewBag.Message = "Failed to generate signed URL.";
                    return View();
                }

                ViewBag.SignedUrl = signedUrl;
            }
            catch (Exception e)
            {
                ViewBag.Message = $"Error: {e.Message}";
            }

            return View();
        }

        private async Task UpdateGoogleCloudPermissions(string fileName, List<ObjectAccessControl> acl)
        {
            var obj = await _storageClient.GetObjectAsync("kms_cloud_storage", fileName);
            obj.Acl = acl;
            await _storageClient.UpdateObjectAsync(obj);
        }
    }
}
