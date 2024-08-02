using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Models;

namespace Google_cloud_storage_solution.Controllers
{
    public class Cloud_File_IndexController : Controller
    {
        private readonly GoogleStorageDbContext _context;

        public Cloud_File_IndexController(GoogleStorageDbContext context)
        {
            _context = context;
        }

        // GET: Cloud_File_Index
        public async Task<IActionResult> Index()
        {
            return View(await _context.cloud_File_Index.ToListAsync());
        }

        private bool Cloud_File_IndexExists(string id)
        {
            return _context.cloud_File_Index.Any(e => e.Id == id);
        }
    }
}
