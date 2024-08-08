using Google_cloud_storage_solution.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Google_cloud_storage_solution.Databases;
using System.Security.Cryptography;
using OfficeOpenXml;
using CsvHelper;

namespace Google_cloud_storage_solution.Controllers
{
    public class AccountController : Controller
    {
        private readonly GoogleStorageDbContext _dbContext;

        public AccountController(GoogleStorageDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private readonly List<object> users = new List<object>();

        // GET: AccountController

        public ActionResult Index()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
        private bool IsAdmin()
        {
            return User.IsInRole("Admin"); // Assuming "Admin" is the role name for admin users
        }

        private bool IsUsernameTaken(string username)
        {
            return _dbContext.Users.Any(user => user.UserName == username);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Register user)
        {
            if (ModelState.IsValid)
            {
                // Check if the provided username is not null or empty
                if (!string.IsNullOrEmpty(user.Username))
                {
                    bool isUsernameTaken = IsUsernameTaken(user.Username);

                    if (isUsernameTaken)
                    {
                        ModelState.AddModelError("Username", "The username is already taken.");
                        return View(user);
                    }

                    var newUser = new Users
                    {
                        UserName = user.Username,
                        Email = user.Email,
                        PasswordHash = user.Password,
                        Fullname = user.Username,
                        PhoneNumber = user.PhoneNumber,
                        PhysicalAddress = user.PhysicalAddress,
                        Role = user.Role,
                    };

                    _dbContext.Users.Add(newUser); // Add the newUser object
                    _dbContext.SaveChanges();

                    // Redirect or perform other actions after successful registration.
                    return RedirectToAction("Login"); // Customize this as needed.
                }
                else
                {
                    // Handle the case where the username is null or empty
                    ModelState.AddModelError("Username", "Username cannot be null or empty.");
                    return View(user);
                }
            }

            return View(user);
        }

        [HttpPost]
        [Route("Account/Login")]
        public async Task<IActionResult> Login(Login model)
        {
            if (ModelState.IsValid)
            {
                var hashedPassword = (model.Password); // Hash the provided password
                var user = _dbContext.Users.FirstOrDefault(u => u.UserName == model.Username && u.PasswordHash == hashedPassword);
                if (user != null)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                    HttpContext.Session.SetString("Username", user.UserName ?? string.Empty);
                    HttpContext.Session.SetString("Role", user.Role ?? string.Empty);

                    var currentTime = DateTime.Now;
                    user.LastLoginTime = currentTime.TimeOfDay;
                    _dbContext.SaveChanges();

                    // Create a new session record
                    var userSession = new UserSessions
                    {
                        UserId = user.UserId,
                        LoginTime = DateTime.Now,
                        IsIdle = false
                    };

                    _dbContext.UserSessions.Add(userSession);
                    _dbContext.SaveChanges();

                    ExportLoginDetailsToExcel(user.UserName, userSession.LoginTime.TimeOfDay);

                    return RedirectToAction("HomePage", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Please provide valid credentials.");
            }

            return View(model);
        }

        [HttpGet]
        [Route("Account/Logout")]
        public async Task<IActionResult> Logout()
        {
            var userName = HttpContext.Session.GetString("Username");
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
                    userSession.DurationHours = duration.Hours;
                    userSession.DurationMinutes = duration.Minutes;
                    _dbContext.SaveChanges();
                    // Export logout details to CSV and Excel
                }
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _dbContext.Users.SingleOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    // Redirect to ResetPassword page with user's email as a parameter
                    return RedirectToAction("ResetPassword", "Account", new { email = model.Email });
                }
                ModelState.AddModelError("", "User with the specified email does not exist.");
            }
            return View(model);
        }


        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _dbContext.Users.SingleOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    user.PasswordHash = (model.Password);
                    _dbContext.SaveChanges();
                    return RedirectToAction("ResetPasswordConfirmation");
                }
                ModelState.AddModelError("", "User with the specified email does not exist.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        private void ExportLoginDetailsToExcel(string userName, TimeSpan loginTime)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", "login_details.xlsx");

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets.Add("LoginDetails");
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                worksheet.Cells[rowCount + 1, 1].Value = userName;
                worksheet.Cells[rowCount + 1, 2].Value = "Login";
                worksheet.Cells[rowCount + 1, 3].Value = loginTime.ToString(@"hh\:mm\:ss");

                package.Save();
            }
        }
    }
}
