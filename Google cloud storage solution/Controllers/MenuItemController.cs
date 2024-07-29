using CsvHelper;
using Google_cloud_storage_solution.Databases;
using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Globalization;

namespace Google_cloud_storage_solution.Controllers
{
    public class UserActivityExitModel
    {
        public int ActivityId { get; set; }
        public string? ExitTime { get; set; }
    }
    public class MenuItemController : Controller
    {
        private readonly GoogleStorageDbContext _dbContext;
        public MenuItemController(GoogleStorageDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _dbContext.MenuItem.ToListAsync());
        }

        public async Task<IActionResult> MenuItemDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _dbContext.MenuItem
                .Include(m => m.Menu)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            var userName = HttpContext.Session.GetString("Username");
            var currentTime = DateTime.Now;

            var userActivity = new UserActivity
            {
                UserName = userName,
                PageName = menuItem.Title,
                EntryTime = currentTime.TimeOfDay,
            };

            _dbContext.UserActivities.Add(userActivity);
            await _dbContext.SaveChangesAsync();

            ExportActivityDetailsToCsv(userActivity);
            ExportActivityDetailsToExcel(userActivity);

            // Pass the activity ID to the view so it can be used for recording the exit time
            ViewBag.ActivityId = userActivity.Id;

            // Pass the list of menus to the view for the dropdown
            ViewData["MenuId"] = new SelectList(_dbContext.Menu, "MenuId", "Title", menuItem.MenuId);
            return View(menuItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MenuItemDetails(int id, [Bind("MenuItemId,MenuId,Title,ActionItems,Assigned,Deadline,Status")] MenuItem menuItem)
        {
            if (id != menuItem.MenuItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(menuItem);
                    await _dbContext.SaveChangesAsync();
                    await ExportMenuItemsToXlsx(menuItem);
                    await ExportTimesheetToXlsx();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuItemExists(menuItem.MenuItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Menu");
            }

            // If we got this far, something failed; redisplay the form.
            ViewData["MenuId"] = new SelectList(_dbContext.Menu, "MenuId", "Title", menuItem.MenuId);
            return View(menuItem);
        }

        private bool MenuItemExists(int id)
        {
            return _dbContext.MenuItem.Any(e => e.MenuItemId == id);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _dbContext.MenuItem
                .Include(m => m.Menu)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        // POST: Menu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menuItem = await _dbContext.MenuItem.FindAsync(id);
            _dbContext.MenuItem.Remove(menuItem);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Menu", "MenuController");
        }

        [HttpPost]
        public IActionResult RecordExitTime()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                var body = reader.ReadToEndAsync().Result;
                var model = JsonConvert.DeserializeObject<UserActivityExitModel>(body);

                var activity = _dbContext.UserActivities.FirstOrDefault(a => a.Id == model.ActivityId);
                if (activity == null)
                {
                    return NotFound("Activity not found.");
                }

                if (DateTime.TryParse(model.ExitTime, null, DateTimeStyles.RoundtripKind, out DateTime exitTimeUtc))
                {
                    // Convert the DateTime to TimeSpan
                    var timeZoneOffset = TimeSpan.FromHours(2); // Change this according to your local time zone
                    var localExitTime = exitTimeUtc.Add(timeZoneOffset);

                    // Convert the adjusted DateTime to TimeSpan
                    activity.ExitTime = localExitTime.TimeOfDay;

                    _dbContext.SaveChanges();

                    // Export entry and exit times to CSV and Excel
                    ExportActivityDetailsToCsv(activity);
                    ExportActivityDetailsToExcel(activity);

                    return Ok("Exit time recorded successfully.");
                }
                else
                {
                    return BadRequest("Invalid exit time format.");
                }
            }
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

        [HttpPost]
        public async Task<IActionResult> ExportMenuItemsToXlsx(MenuItem menuItem)
        {
            var menuItems = await _dbContext.MenuItem.Include(m => m.Menu).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("MenuItems");
                var currentRow = 1;

                worksheet.Cells[currentRow, 1].Value = "MenuItemId";
                worksheet.Cells[currentRow, 2].Value = "MenuId";
                worksheet.Cells[currentRow, 3].Value = "Title";
                worksheet.Cells[currentRow, 4].Value = "Url";
                worksheet.Cells[currentRow, 5].Value = "ActionItems";
                worksheet.Cells[currentRow, 6].Value = "Assigned";
                worksheet.Cells[currentRow, 7].Value = "Deadline";
                worksheet.Cells[currentRow, 8].Value = "Status";

                foreach (var item in menuItems)
                {
                    currentRow++;
                    worksheet.Cells[currentRow, 1].Value = item.MenuItemId;
                    worksheet.Cells[currentRow, 2].Value = item.MenuId;
                    worksheet.Cells[currentRow, 3].Value = item.Title;
                    worksheet.Cells[currentRow, 4].Value = item.Url;
                    worksheet.Cells[currentRow, 5].Value = item.ActionItems;
                    worksheet.Cells[currentRow, 6].Value = item.Assigned;
                    worksheet.Cells[currentRow, 7].Value = item.Deadline;
                    worksheet.Cells[currentRow, 8].Value = item.Status;
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "menuitems.xlsx");
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    package.SaveAs(stream);
                }
                return Ok("Excel file saved successfully.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportTimesheetToXlsx()
        {
            var currenttime = DateTime.Now;
            var activities = await _dbContext.UserActivities.ToListAsync();
            var menuItems = await _dbContext.MenuItem.ToListAsync();
            var currentDate = DateTime.Now;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Timesheet");
                var currentRow = 1;

                worksheet.Cells[currentRow, 1].Value = "Week Number";
                worksheet.Cells[currentRow, 2].Value = "Name of the Project";
                worksheet.Cells[currentRow, 3].Value = "Deliverable";
                worksheet.Cells[currentRow, 4].Value = "Description of Activity";
                worksheet.Cells[currentRow, 5].Value = "Output";
                worksheet.Cells[currentRow, 6].Value = "Start Time";
                worksheet.Cells[currentRow, 7].Value = "End Time";
                worksheet.Cells[currentRow, 8].Value = "Duration";

                foreach (var activity in activities)
                {
                    var menuItem = menuItems.FirstOrDefault(m => m.Title == activity.PageName); // Assuming PageName is used for matching
                    if (menuItem != null)
                    {
                        currentRow++;
                        var weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(currentDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday); ;
                        var startTime = activity.EntryTime.ToString(@"hh\:mm\:ss");
                        var endTime = activity.ExitTime.ToString(@"hh\:mm\:ss");
                        var duration = (activity.ExitTime - activity.EntryTime).ToString(@"hh\:mm\:ss");

                        worksheet.Cells[currentRow, 1].Value = weekNumber;
                        worksheet.Cells[currentRow, 2].Value = menuItem.Title;
                        worksheet.Cells[currentRow, 3].Value = menuItem.ActionItems;
                        worksheet.Cells[currentRow, 4].Value = string.Empty;
                        worksheet.Cells[currentRow, 5].Value = menuItem.Status;
                        worksheet.Cells[currentRow, 6].Value = startTime;
                        worksheet.Cells[currentRow, 7].Value = endTime;
                        worksheet.Cells[currentRow, 8].Value = duration;
                    }
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "timesheet.xlsx");
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await package.SaveAsAsync(stream);
                }

                return Ok("Excel timesheet file saved successfully.");
            }
        }
    }
}
