using CsvHelper;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Diagnostics;
using System.Globalization;
using Google_cloud_storage_solution.Databases;

namespace Google_cloud_storage_solution.Controllers
{
    public class HomeController : Controller
    {
        public int ActivityId { get; set; }

        private readonly ILogger<HomeController> _logger;
        private readonly GoogleStorageDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, GoogleStorageDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            TimeTracking(nameof(Privacy));
            return View();
        }

        public IActionResult HomePage()
        {
           
            TimeTracking(nameof(HomePage));
            var role = "Admin"; // Or retrieve the actual role from your authentication system
            ViewData["Role"] = role;
            return View();

        }

        public IActionResult AboutUs()
        {
            TimeTracking(nameof(AboutUs));
            return View();
        }

        public IActionResult Contact()
        {
            TimeTracking(nameof(Contact));
            return View();
        }

        public IActionResult Terms()
        {
            TimeTracking(nameof(Terms));
            return View();
        }

        public IActionResult Account()
        {
            TimeTracking(nameof(Account));
            var userName = HttpContext.User.Identity.Name;
            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userName);
            return View(user);
        }

        public IActionResult Inbox()
        {
            TimeTracking(nameof(Inbox));
            return View();
        }

        public IActionResult Settings()
        {
            TimeTracking(nameof(Settings));
            var userName = HttpContext.User.Identity.Name;
            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userName);
            return View(user);
        }

        public IActionResult Logout()
        {
            var userName = HttpContext.User.Identity.Name;
            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userName);

            if (user != null)
            {
                // Calculate duration
                var currentTime = DateTime.Now;
                var userSession = _dbContext.UserSessions
                    .Where(us => us.UserId == user.UserId && us.LogoutTime == null)
                    .OrderByDescending(us => us.LoginTime)
                    .FirstOrDefault();

                if (userSession != null)
                {
                    var loginTime = userSession.LoginTime;
                    var duration = currentTime - loginTime;

                    // Store logout time and duration
                    userSession.LogoutTime = currentTime;
                    userSession.Duration = duration;
                    userSession.DurationHours = (int)duration.Hours;
                    userSession.DurationMinutes = duration.Minutes;
                    _dbContext.SaveChanges();

                    // Export logout details to CSV and Excel
                    ExportLogoutDetailsToExcel(user.UserName, userSession.LogoutTime.Value.TimeOfDay, userSession.Duration.Value);
                }
            }

            // Add any additional logout logic or redirection here
            // Clear session data
            HttpContext.Session.Clear();

            // Manually remove the authentication cookies
            Response.Cookies.Delete(".AspNetCore.Cookies");

            // Sign out the user
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the logged out page
            return RedirectToAction("LogOut", "Account");
        }


        private void ExportLogoutDetailsToExcel(string userName, TimeSpan logoutTime, TimeSpan duration)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "logout_details.xlsx");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets.Add("LogoutDetails");
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                worksheet.Cells[rowCount + 1, 1].Value = userName;
                worksheet.Cells[rowCount + 1, 2].Value = logoutTime.ToString(@"hh\:mm\:ss");
                worksheet.Cells[rowCount + 1, 3].Value = duration.ToString(@"hh\:mm");

                package.Save();
            }
        }

        public IActionResult LoggedOut()
        {
            return View();
        }

        private void ExportActivityDetailsToExcel(UserActivity activity)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "login_details.xlsx");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets.Add("ActivityDetails");
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                worksheet.Cells[rowCount + 1, 1].Value = activity.UserName;
                worksheet.Cells[rowCount + 1, 2].Value = activity.PageName;
                worksheet.Cells[rowCount + 1, 3].Value = activity.EntryTime.ToString(@"hh\:mm\:ss");
                worksheet.Cells[rowCount + 1, 4].Value = activity.ExitTime.ToString(@"hh\:mm\:ss");

                package.Save();
            }
        }
        private void TimeTracking(string pageName)
        {
            var userName = HttpContext.User.Identity.Name;
            var currentTime = DateTime.Now;

            var userActivity = new UserActivity
            {
                UserName = userName,
                PageName = pageName,
                EntryTime = currentTime.TimeOfDay,
            };

            _dbContext.UserActivities.Add(userActivity);
            _dbContext.SaveChanges();

            ExportActivityDetailsToExcel(userActivity);

            // Pass the activity ID to the view so it can be used for recording the exit time
            ViewBag.ActivityId = userActivity.Id;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
