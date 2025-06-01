using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Aryaans_Hotel_Booking.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View(); 
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogInformation($"User {email} logged in successfully.");
                    TempData["SuccessMessage"] = "Login successful! Welcome back.";
                    HttpContext.Session.SetString("Username", user.Username);
                    return RedirectToAction("Index", "Home"); 
                }
            }

            _logger.LogWarning($"Login attempt failed for user {email}. Invalid credentials.");
            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
            return View(); 
        }
        [HttpGet] 
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");
            TempData["SuccessMessage"] = "You have been successfully logged out."; // Optional: Add a success message
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                return View(); 
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
            }

            bool usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "This username is already taken. Please choose another one.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {email} registered successfully with username {username}.");
            TempData["SuccessMessage"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login"); 
        }
    }
}
