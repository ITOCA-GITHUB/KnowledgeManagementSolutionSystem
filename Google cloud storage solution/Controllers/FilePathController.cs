using Google_cloud_storage_solution.Databases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Google_cloud_storage_solution.Controllers
{
    public class FilePathController : Controller
    {
        private readonly GoogleStorageDbContext _context;
        public FilePathController(GoogleStorageDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var filePaths = await _context.file_paths.ToListAsync();
            return View(filePaths);
        }
    }
}
