using Google.Apis.Drive.v3.Data;
using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Google_cloud_storage_solution.Controllers
{
    public class UsersController : Controller
    {
        private readonly GoogleStorageDbContext _dbContext;
        public UsersController(GoogleStorageDbContext dbContext)
        {
            _dbContext = dbContext;  
        }
        public IActionResult Index()
        {
            var users = _dbContext.Users.ToList();
            return View(users);
        }

        public ActionResult Details(int id)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Users user)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();
                return RedirectToAction(nameof(Details), new { id = user.UserId });
            }
            return View(user);
        }

        public ActionResult Edit(int id)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Users user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _dbContext.Users.Update(user);
                _dbContext.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public ActionResult Delete(int id)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
                _dbContext.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
