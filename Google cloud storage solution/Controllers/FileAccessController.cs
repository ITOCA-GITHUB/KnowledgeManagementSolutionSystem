using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Mvc;
namespace Google_cloud_storage_solution.Controllers;

public class FileAccessController : Controller
{
    private readonly StorageClient _storageClient;

    public FileAccessController()
    {
        var credential = GoogleCredential.FromFile("GoogleStorageBucket.json");
        _storageClient = StorageClient.Create(credential);
    }

    [HttpGet]
    public IActionResult EditAccess(string fileName)
    {
        var model = new EditAccessViewModel
        {
            FileName = fileName
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditAccess(EditAccessViewModel model)
    {
        try
        {
            // Ensure the bucket and object names are correct
            var bucketName = "kms_cloud_storage";
            var fileName = model.FileName;

            // Fetch the object
            var storageObject = await _storageClient.GetObjectAsync(bucketName, fileName);

            // Validate and set the role
            string role = model.AccessType.ToUpper();
            if (role != "OWNER" && role != "READER" && role != "WRITER")
            {
                ViewBag.Message = "Invalid role specified.";
                return View(model);
            }

            // Update ACL
            var acl = new List<ObjectAccessControl>
            {
                new ObjectAccessControl
                {
                    Entity = $"user-{model.UserEmail}",
                    Role = role
                }
            };

            storageObject.Acl = acl;
            await _storageClient.UpdateObjectAsync(storageObject);

            ViewBag.Message = "Access updated successfully!";
        }
        catch (Google.GoogleApiException e)
        {
            ViewBag.Message = $"Error: {e.Message}";
        }

        return View(model);
    }

    private async Task UpdateGoogleCloudPermissions(string fileName, List<ObjectAccessControl> acl)
    {
        var obj = await _storageClient.GetObjectAsync("kms_cloud_storage", fileName);
        obj.Acl = acl;
        await _storageClient.UpdateObjectAsync(obj);
    }
}
