using Aryaans_Hotel_Booking.Areas.Admin.Models;
using Aryaans_Hotel_Booking.Data.Entities; // Required for ApplicationUser
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Required for UserManager and RoleManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for ToListAsync()
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims; // Required for User.FindFirstValue

namespace Aryaans_Hotel_Booking.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Only Admins can access this controller
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/UserManagement/Index
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var user in users)
            {
                userViewModels.Add(new UserViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = await _userManager.GetRolesAsync(user),
                    EmailConfirmed = user.EmailConfirmed,
                });
            }
            return View(userViewModels);
        }

        // POST: /Admin/UserManagement/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["UserMessage"] = "User ID cannot be empty.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == currentUserId)
            {
                TempData["UserMessage"] = "You cannot delete your own account from this interface.";
                TempData["MessageType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["UserMessage"] = "User not found.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["UserMessage"] = $"User '{user.UserName}' deleted successfully.";
                TempData["MessageType"] = "success";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["UserMessage"] = $"Error deleting user '{user.UserName}': {errors}";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
