using Google_cloud_storage_solution.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
namespace Google_cloud_storage_solution.Controllers;
public class StorageController : Controller
{
    private readonly GoogleCloudStorageService _cloudStorageService;

    public StorageController(GoogleCloudStorageService cloudStorageService)
    {
        _cloudStorageService = cloudStorageService;
    }

    public async Task<IActionResult> Index(string prefix = "")
    {
        var bucketName = "kms_cloud_storage";
        var storageObjects = await _cloudStorageService.ListObjectsAsync(bucketName, prefix);
        ViewBag.Prefix = prefix;
        return View(storageObjects);
    }
}
