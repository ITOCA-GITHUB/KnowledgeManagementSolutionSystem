using CsvHelper;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            return View();
        }

        public IActionResult Logout()
        {
            var userName = HttpContext.User.Identity.Name;
            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userName);

            if (user != null)
            {
                // Calculate duration
                var currentTime = DateTime.Now.TimeOfDay;
                var loginTime = _dbContext.UserSessions
                    .Where(us => us.UserId == user.UserId && us.LogoutTime == null)
                    .OrderByDescending(us => us.LoginTime)
                .Select(us => us.LoginTime)
                    .FirstOrDefault();

                var duration = currentTime - loginTime;

                // Store logout time and duration
                var userSession = _dbContext.UserSessions
                    .Where(us => us.UserId == user.UserId && us.LogoutTime == null)
                    .OrderByDescending(us => us.LoginTime)
                    .FirstOrDefault();

                if (userSession != null)
                {
                    userSession.LogoutTime = currentTime;
                    userSession.Duration = new TimeSpan(duration.Hours, duration.Minutes, 0);
                    _dbContext.SaveChanges();

                    // Export logout details to CSV and Excel
                    ExportLogoutDetailsToCsv(user.UserName, userSession.LogoutTime.Value, userSession.Duration.Value);
                    ExportLogoutDetailsToExcel(user.UserName, userSession.LogoutTime.Value, userSession.Duration.Value);
                }
            }

            // Clear session data
            HttpContext.Session.Clear();

            // Manually remove the authentication cookies
            Response.Cookies.Delete(".AspNetCore.Cookies");

            // Sign out the user
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the logged out page
            return RedirectToAction("LogOut", "Account");
        }

        private void ExportLogoutDetailsToCsv(string userName, TimeSpan logoutTime, TimeSpan duration)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "logout_details.csv");
            var logoutDetails = new
            {
                UserName = userName,
                LogoutTime = logoutTime.ToString(@"hh\:mm\:ss"),
                Duration = duration.ToString(@"hh\:mm")
            };

            using (var writer = new StreamWriter(filePath, append: true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecord(logoutDetails);
                writer.WriteLine(); // Ensures each record is on a new line
            }
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

        private void ExportActivityDetailsToCsv(UserActivity activity)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "login_details.csv");
            var activityDetails = new
            {
                UserName = activity.UserName,
                PageName = activity.PageName,
                EntryTime = activity.EntryTime.ToString(@"hh\:mm\:ss"),
                ExitTime = activity.ExitTime.ToString(@"hh\:mm\:ss")
            };

            using (var writer = new StreamWriter(filePath, append: true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecord(activityDetails);
                writer.WriteLine(); // Ensures each record is on a new line
            }
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


            ExportActivityDetailsToCsv(userActivity);
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
