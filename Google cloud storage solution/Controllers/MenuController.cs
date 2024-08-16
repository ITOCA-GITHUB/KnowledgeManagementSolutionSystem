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

        // Get list of menus for deletion
        public IActionResult DeleteMenu()
        {
            var menus = _context.Menu.ToList();
            return View(menus);
        }

        [HttpPost]
        public IActionResult DeleteMenu(int id)
        {
            try
            {
                var menu = _context.Menu.Find(id);
                if (menu != null)
                {
                    var relatedItems = _context.MenuItem.Where(mi => mi.MenuId == id).ToList();

                    if (relatedItems.Any())
                    {
                        // Handle the case where there are related menu items
                        ModelState.AddModelError(string.Empty, "An error occurred while trying to delete the menu. Please try again later.");

                    }

                    _context.Menu.Remove(menu);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex.Message);
                // Provide user feedback
                ModelState.AddModelError(string.Empty,"An error occurred while trying to delete the menu. Please try again later.");
            }

            return RedirectToAction("DeleteMenu");
        }


        // Get list of menu items for deletion
        public IActionResult DeleteMenuItem()
        {
            var menuItems = _context.MenuItem.ToList();
            return View(menuItems);
        }

        // Delete selected menu item
        [HttpPost]
        public IActionResult DeleteMenuItem(int id)
        {
            var menuItem = _context.MenuItem.Find(id);
            if (menuItem != null)
            {
                _context.MenuItem.Remove(menuItem);
                _context.SaveChanges();
            }
            return RedirectToAction("DeleteMenuItem");
        }
    }
}
