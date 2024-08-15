using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Google_cloud_storage_solution.Controllers
{
    public class MenuController : Controller
    {
        private readonly GoogleStorageDbContext _context;
        public MenuController(GoogleStorageDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var menus = await _context.Menu
                .Include(m => m.MenuItems)
                .ToListAsync();

            return View(menus);
        }

        public IActionResult CreateMenu()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateMenu(CreateMenuViewModel model)
        {
            if (ModelState.IsValid)
            {
                var menu = new Menu { Title = model.Title };
                _context.Menu.Add(menu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> CreateMenuItem()
        {
            var menus = await _context.Menu.ToListAsync();
            ViewBag.Menus = menus;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateMenuItem(CreateMenuItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                var menuItem = new MenuItem
                {
                    MenuId = model.MenuId,
                    Title = model.Title,
                    Url = model.Url
                };
                _context.MenuItem.Add(menuItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var menus = await _context.Menu.ToListAsync();
            ViewBag.Menus = menus;
            return View(model);
        }

        // GET: /Menu/DeleteMenu/5
        [HttpGet]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await _context.Menu
                .Include(m => m.MenuItems)
                .FirstOrDefaultAsync(m => m.MenuId == id);

            if (menu == null)
            {
                return NotFound();
            }

            return View(menu);
        }

        public async Task<IActionResult> DeleteMenuConfirmed(int id)
        {
            var menu = await _context.Menu.FindAsync(id);
            if (menu != null)
            {
                _context.Menu.Remove(menu);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Menu/DeleteMenuItem/5
        [HttpGet]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItem
                .FirstOrDefaultAsync(mi => mi.MenuItemId == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        public async Task<IActionResult> DeleteMenuItemConfirmed(int id)
        {
            var menuItem = await _context.MenuItem.FindAsync(id);
            if (menuItem != null)
            {
                _context.MenuItem.Remove(menuItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
