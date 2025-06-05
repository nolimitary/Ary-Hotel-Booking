using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace Aryaans_Hotel_Booking.Areas.Admin.Controllers
{
    [Area("Admin")] 
    [Authorize(Roles = "Admin")] 
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Message"] = "Welcome to the Admin Dashboard!";
            return View();
        }

    }
}
