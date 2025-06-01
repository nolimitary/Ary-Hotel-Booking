using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
        public IActionResult Login(string? returnUrl = null)
        {
            var viewModel = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model) 
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    if (BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    {
                        _logger.LogInformation($"User {model.Email} logged in successfully.");
                        TempData["SuccessMessage"] = "Login successful! Welcome back.";
                        HttpContext.Session.SetString("Username", user.Username);

                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
                _logger.LogWarning($"Login attempt failed for user {model.Email}. Invalid credentials.");
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");
            TempData["SuccessMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation("--- Register POST action started ---");
            _logger.LogInformation($"Attempting to register with Username: '{model.Username}', Email: '{model.Email}'");

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Basic client-side style validations passed. Checking database for existing email/username.");
                bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    _logger.LogWarning($"Register validation FAILED: Email '{model.Email}' already exists in DB.");
                }

                bool usernameExists = await _context.Users.AnyAsync(u => u.Username == model.Username);
                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "This username is already taken. Please choose another one.");
                    _logger.LogWarning($"Register validation FAILED: Username '{model.Username}' already exists in DB.");
                }

                if (!emailExists && !usernameExists)
                {
                    _logger.LogInformation("--- ModelState is VALID and no existing user found. Proceeding to create and save user. ---");
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
                    };

                    try
                    {
                        _logger.LogInformation("Adding user to context...");
                        _context.Users.Add(user);
                        _logger.LogInformation("Calling SaveChangesAsync...");
                        int changes = await _context.SaveChangesAsync();
                        _logger.LogInformation($"SaveChangesAsync completed. {changes} state entries written to the database.");

                        if (changes > 0)
                        {
                            _logger.LogInformation($"User '{model.Email}' (Username: '{model.Username}') registered successfully. New User ID: {user.Id}");
                            TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                            return RedirectToAction("Login");
                        }
                        else
                        {
                            _logger.LogWarning("SaveChangesAsync reported 0 changes. User might not have been saved.");
                            ModelState.AddModelError(string.Empty, "Could not save the user. Please try again.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "--- EXCEPTION during SaveChangesAsync or user creation. ---");
                        ModelState.AddModelError(string.Empty, "An unexpected error occurred while registering. Please try again.");
                        _logger.LogError($"Exception Details: {ex.ToString()}");
                        if (ex.InnerException != null)
                        {
                            _logger.LogError($"Inner Exception Details: {ex.InnerException.ToString()}");
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("--- ModelState is INVALID. Returning View with errors. ---");
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var modelStateVal = ModelState[modelStateKey];
                    foreach (var error in modelStateVal.Errors)
                    {
                        _logger.LogWarning($"ModelState Error -> Key: '{modelStateKey}', Error: '{error.ErrorMessage}'");
                    }
                }
            }
            return View(model);
        }
    }
}