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
            _logger.LogInformation("--- Register POST action started ---");
            _logger.LogInformation($"Attempting to register with Username: '{username}', Email: '{email}'");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                _logger.LogWarning("Register validation FAILED: All fields are required check.");
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                _logger.LogWarning("Register validation FAILED: Passwords do not match check.");
            }

            // Perform DB checks only if basic client-side style validations seem okay so far
            if (ModelState.ErrorCount == 0) // Only check DB if other simple validations passed
            {
                _logger.LogInformation("Basic client-side style validations passed. Checking database for existing email/username.");
                bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    _logger.LogWarning($"Register validation FAILED: Email '{email}' already exists in DB.");
                }

                bool usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "This username is already taken. Please choose another one.");
                    _logger.LogWarning($"Register validation FAILED: Username '{username}' already exists in DB.");
                }
            }
            else
            {
                _logger.LogWarning("Skipping DB checks for existing email/username due to prior ModelState errors.");
            }


            if (!ModelState.IsValid)
            {
                _logger.LogWarning("--- ModelState is INVALID. Returning View with errors. ---");
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var modelStateVal = ModelState[modelStateKey];
                    foreach (var error in modelStateVal.Errors)
                    {
                        // Log the key and the error message.
                        _logger.LogWarning($"ModelState Error -> Key: '{modelStateKey}', Error: '{error.ErrorMessage}'");
                    }
                }
                return View(); // Return to the view with validation messages
            }

            _logger.LogInformation("--- ModelState is VALID. Proceeding to create and save user. ---");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                // Id is not set here; it should be auto-generated by the database.
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
                    _logger.LogInformation($"User '{email}' (Username: '{username}') registered successfully. New User ID: {user.Id}");
                    TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    _logger.LogWarning("SaveChangesAsync reported 0 changes. User might not have been saved.");
                    ModelState.AddModelError(string.Empty, "Could not save the user. Please try again.");
                    return View(); // Stay on registration page with an error
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "--- EXCEPTION during SaveChangesAsync or user creation. ---");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while registering. Please try again.");
                // Log detailed exception information
                _logger.LogError($"Exception Details: {ex.ToString()}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception Details: {ex.InnerException.ToString()}");
                }
                return View(); // Return to view with error
            }
        }
    }
    }
