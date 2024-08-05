using Google_cloud_storage_solution.Databases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Google_cloud_storage_solution.Controllers
{
    public class FileIndexController : Controller
    {
        private readonly GoogleStorageDbContext _context;

        public FileIndexController(GoogleStorageDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string fileType, string fileName, string sortOrder)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(); // Return 401 Unauthorized if the user is not authenticated
            }

            var user = await _context.Users.Include(u => u.RolePermissions).FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null || !user.RolePermissions.Any(rp => rp.CanView))
            {
                return Forbid(); // Return 403 Forbidden if the user does not have permission to view files
            }

            var query = _context.file_index.AsQueryable();

            // Map user-friendly file types to MIME types
            var mimeTypes = new Dictionary<string, string>
            {
                { "pdf", "application/pdf" },
                { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { "jpg", "image/jpeg" },
                { "mkv", "video/x-matroska" },
                { "doc", "application/msword" },
                // Add other mappings as needed
            };

            // Filter by file type
            if (!string.IsNullOrEmpty(fileType) && mimeTypes.ContainsKey(fileType.ToLower()))
            {
                var mimeType = mimeTypes[fileType.ToLower()];
                query = query.Where(f => f.file_type == mimeType);
            }

            // Search by name
            if (!string.IsNullOrEmpty(fileName))
            {
                query = query.Where(f => f.name.Contains(fileName));
            }

            // Sorting
            switch (sortOrder)
            {
                case "name_asc":
                    query = query.OrderBy(f => f.name);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(f => f.name);
                    break;
                default:
                    query = query.OrderBy(f => f.date_created); // Default sorting by date
                    break;
            }

            var fileIndexes = await query.ToListAsync();
            return View(fileIndexes);
        }
    }
}
