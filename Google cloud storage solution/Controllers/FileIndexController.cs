using Google_cloud_storage_solution.Databases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Google_cloud_storage_solution.Controllers
{
    public class FileIndexController : Controller
    {
        private readonly GoogleStorageDbContext _context;
        public FileIndexController(GoogleStorageDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var fileIndexes = await _context.file_index.ToListAsync();
            return View(fileIndexes);
        }
    }
}
