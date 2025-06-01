using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace Aryaans_Hotel_Booking.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"User {model.Email} logged in successfully.");
                        TempData["SuccessMessage"] = "Login successful! Welcome back.";
                        HttpContext.Session.SetString("Username", user.UserName);

                        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }
                        return RedirectToAction("Index", "Home");
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning($"User {model.Email} account locked out.");
                        ModelState.AddModelError(string.Empty, "This account has been locked out, please try again later.");
                        return View(model);
                    }
                }
                _logger.LogWarning($"Login attempt failed for user {model.Email}. Invalid credentials or user not found.");
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userNameForLog = User.Identity?.Name ?? HttpContext.Session.GetString("Username") ?? "Unknown user";
            await _signInManager.SignOutAsync();
            HttpContext.Session.Remove("Username");
            TempData["SuccessMessage"] = "You have been successfully logged out.";
            _logger.LogInformation($"User {userNameForLog} logged out.");
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
            _logger.LogInformation($"Registration attempt for {model.Email}.");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {model.Email} (Username: {model.Username}) created a new account.");


                    TempData["SuccessMessage"] = "Registration successful! Please check your email if confirmation is required, then log in.";
                    return RedirectToAction("Login");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogWarning($"Registration error for {model.Email}: {error.Description}");
                }
            }

            _logger.LogWarning($"Registration failed for {model.Email}. ModelState Invalid or user creation failed.");
            return View(model);
        }
    }
}