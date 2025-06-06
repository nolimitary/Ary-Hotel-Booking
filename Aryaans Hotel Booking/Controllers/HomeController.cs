using Aryaans_Hotel_Booking.Models;
using Aryaans_Hotel_Booking.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Aryaans_Hotel_Booking.Data;
using Microsoft.EntityFrameworkCore;

namespace Aryaans_Hotel_Booking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHotelService _hotelService;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IHotelService hotelService, ApplicationDbContext context)
        {
            _logger = logger;
            _hotelService = hotelService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? selectedDates, string? selectedGuests, string? selectedLocation)
        {
            ViewData["SelectedDates"] = selectedDates;
            ViewData["SelectedGuests"] = !string.IsNullOrEmpty(selectedGuests) ? WebUtility.UrlDecode(selectedGuests) : null;
            ViewData["SelectedLocation"] = !string.IsNullOrEmpty(selectedLocation) ? WebUtility.UrlDecode(selectedLocation) : null;

            var featuredHotelViewModels = await _hotelService.GetFeaturedHotelsAsync(3);
            return View(featuredHotelViewModels);
        }

        public async Task<IActionResult> SearchResults(
            string? destination, string? selectedDates, string? selectedGuests,
            int? minRating, decimal? minPrice, decimal? maxPrice,
            string? sortBy = "name", string? sortOrder = "asc", int pageNumber = 1)
        {
            _logger.LogInformation($"Controller: Search Request: Dest='{destination}', SortBy='{sortBy}', Page='{pageNumber}'");

            var viewModel = await _hotelService.SearchHotelsAsync(
                destination, selectedDates, selectedGuests,
                minRating, minPrice, maxPrice,
                sortBy, sortOrder, pageNumber, 6);

            return View("Results", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AddHotel()
        {
            _logger.LogInformation("Admin user accessing AddHotel GET page via controller.");
            var viewModel = await _hotelService.PrepareAddHotelViewModelAsync();
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHotel(AddHotelViewModel model)
        {
            _logger.LogInformation($"Admin user attempting to POST to AddHotel with hotel name: {model.Name} via controller.");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddHotel POST request failed model validation in controller.");
                return View(model);
            }

            var hotelId = await _hotelService.AddHotelAsync(model);

            if (hotelId)
            {
                TempData["SuccessMessage"] = $"Hotel '{model.Name}' added successfully!";
                return RedirectToAction("SearchResults", new { destination = model.Country });
            }
            else
            {
                ModelState.AddModelError("", "An error occurred while saving the hotel. The image might have failed to save or there was a database issue.");
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditHotel(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("EditHotel GET called with null ID by admin via controller.");
                return NotFound();
            }
            var viewModel = await _hotelService.GetHotelForEditAsync(id.Value);
            if (viewModel == null)
            {
                _logger.LogWarning($"EditHotel GET: Hotel with ID {id} not found for admin edit via controller.");
                return NotFound();
            }
            _logger.LogInformation($"Admin loading EditHotel page for Hotel ID {id} via controller.");
            return View("EditHotel", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHotel(int id, EditHotelViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                _logger.LogWarning($"EditHotel POST ID mismatch in controller. Route ID: {id}, ViewModel ID: {viewModel.Id}.");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                bool success = await _hotelService.UpdateHotelAsync(viewModel);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Hotel '{viewModel.Name}' updated successfully!";
                    _logger.LogInformation($"Hotel ID {viewModel.Id} ('{viewModel.Name}') updated by admin via controller.");
                    var hotel = await _hotelService.GetHotelByIdForApiAsync(id);
                    return RedirectToAction(nameof(SearchResults), new { destination = hotel?.Country ?? "" });
                }
                else
                {
                    ModelState.AddModelError("", "An error occurred while updating the hotel. It might have been modified by another user, the image failed to save, or there was a database issue.");
                }
            }

            _logger.LogWarning($"EditHotel POST: ModelState is invalid for Hotel ID {id} in controller.");
            if (string.IsNullOrEmpty(viewModel.ExistingImagePath) && id > 0)
            {
                var existingHotelModel = await _hotelService.GetHotelForEditAsync(id);
                if (existingHotelModel != null) viewModel.ExistingImagePath = existingHotelModel.ExistingImagePath;
            }
            return View("EditHotel", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> DeleteHotel(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("DeleteHotel GET called with null ID by admin via controller.");
                return NotFound();
            }
            var hotel = await _hotelService.GetHotelForDeletionAsync(id.Value);
            if (hotel == null)
            {
                _logger.LogWarning($"DeleteHotel GET: Hotel with ID {id} not found for admin deletion confirmation via controller.");
                return NotFound();
            }
            _logger.LogInformation($"Admin displaying delete confirmation for Hotel ID {id}, Name: {hotel.Name} via controller.");
            return View(hotel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("DeleteHotel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHotelConfirmed(int id)
        {
            var hotel = await _hotelService.GetHotelByIdForApiAsync(id);
            if (hotel == null)
            {
                TempData["ErrorMessage"] = $"Hotel with ID {id} not found for deletion.";
                return RedirectToAction(nameof(SearchResults));
            }

            var (success, oldImagePath) = await _hotelService.DeleteHotelAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = $"Hotel '{hotel.Name}' was successfully deleted.";
                _logger.LogInformation($"Hotel ID {id}, Name: {hotel.Name} deleted by admin via controller.");
            }
            else
            {
                TempData["ErrorMessage"] = $"Error deleting hotel '{hotel.Name}'. It might have existing bookings or another issue occurred.";
                return RedirectToAction(nameof(DeleteHotel), new { id = id });
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult DatePicker()
        {
            var startDate = DateTime.Today;
            int numberOfMonths = 12;
            var viewModel = new DatePickerViewModel { StartDate = startDate, NumberOfMonths = numberOfMonths };

            DateTime currentMonthDate = new DateTime(startDate.Year, startDate.Month, 1);
            for (int i = 0; i < numberOfMonths; i++)
            {
                var monthViewModel = new MonthViewModel
                {
                    Year = currentMonthDate.Year,
                    Month = currentMonthDate.Month,
                    MonthName = currentMonthDate.ToString("MMMM", CultureInfo.InvariantCulture)
                };

                DayOfWeek firstDayOfMonth = currentMonthDate.DayOfWeek;
                int placeholders = ((int)firstDayOfMonth - (int)DayOfWeek.Monday + 7) % 7;
                for (int p = 0; p < placeholders; p++)
                {
                    monthViewModel.Days.Add(new DayViewModel { IsPlaceholder = true });
                }

                int daysInMonth = DateTime.DaysInMonth(currentMonthDate.Year, currentMonthDate.Month);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var dayDate = new DateTime(currentMonthDate.Year, currentMonthDate.Month, day);
                    monthViewModel.Days.Add(new DayViewModel
                    {
                        DayNumber = day,
                        Date = dayDate,
                        IsToday = (dayDate == DateTime.Today),
                        IsDisabled = (dayDate < DateTime.Today)
                    });
                }
                viewModel.Months.Add(monthViewModel);
                currentMonthDate = currentMonthDate.AddMonths(1);
            }
            return View(viewModel);
        }
        public IActionResult GuestPicker() => View();
        public IActionResult DestinationPicker() => View();

        public async Task<IActionResult> BookingDetails(string hotelName, string selectedDates, int numberOfGuests = 1)
        {
            var decodedHotelName = WebUtility.UrlDecode(hotelName ?? "");
            var hotelEntities = await _hotelService.GetAllHotelsForApiAsync();
            var targetHotel = hotelEntities.FirstOrDefault(h => h.Name.Equals(decodedHotelName, StringComparison.OrdinalIgnoreCase));

            if (targetHotel == null)
            {
                TempData["ErrorMessage"] = "Hotel not found.";
                return RedirectToAction(nameof(Index));
            }

            DateTime checkIn = DateTime.Today.AddDays(1);
            DateTime checkOut = DateTime.Today.AddDays(2);
            string displayDates = "Next available";

            if (!string.IsNullOrEmpty(selectedDates))
            {
                var decodedDates = WebUtility.UrlDecode(selectedDates);
                displayDates = decodedDates;
                try
                {
                    var dates = decodedDates.Split(new[] { " to " }, StringSplitOptions.RemoveEmptyEntries);
                    if (dates.Length == 2)
                    {
                        if (DateTime.TryParse(dates[0], out var parsedCheckIn) &&
                            DateTime.TryParse(dates[1], out var parsedCheckOut))
                        {
                            if (parsedCheckIn >= DateTime.Today && parsedCheckOut > parsedCheckIn)
                            {
                                checkIn = parsedCheckIn;
                                checkOut = parsedCheckOut;
                                displayDates = $"{checkIn:MMM dd, yyyy} - {checkOut:MMM dd, yyyy}";
                            }
                        }
                    }
                }
                catch { }
            }

            int nights = (checkOut - checkIn).Days > 0 ? (checkOut - checkIn).Days : 1;
            int reviewCountForHotel = await _context.Bookings.CountAsync(b => b.HotelId == targetHotel.Id);

            var vm = new BookingViewModel
            {
                HotelName = targetHotel.Name,
                ImageUrl = targetHotel.ImagePath,
                StarRating = targetHotel.StarRating,
                LocationName = $"{targetHotel.City}, {targetHotel.Country}",
                PricePerNight = targetHotel.PricePerNight,
                ReviewScore = (decimal)(targetHotel.ReviewScore ?? 0),
                ReviewCount = reviewCountForHotel,
                CurrencySymbol = "BGN",
                SelectedDates = displayDates,
                SelectedGuests = $"{numberOfGuests} Guest(s)",
                NumberOfNights = nights,
                TotalPrice = nights * targetHotel.PricePerNight,
                DistanceFromCenter = "N/A",
                ReviewScoreText = GetReviewText((decimal)(targetHotel.ReviewScore ?? 0))
            };

            ViewData["HotelId"] = targetHotel.Id;
            ViewData["CheckInDate"] = checkIn.ToString("yyyy-MM-dd");
            ViewData["CheckOutDate"] = checkOut.ToString("yyyy-MM-dd");
            ViewData["NumberOfGuests"] = numberOfGuests;

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(CreateBookingViewModel model)
        {
            ModelState.Clear();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["UserMessage"] = "You must be logged in to make a booking.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Account", new
                {
                    returnUrl = Url.Action("BookingDetails", "Home", new
                    {
                        hotelName = WebUtility.UrlEncode(model.HotelName),
                        selectedDates = WebUtility.UrlEncode($"{model.CheckInDate:yyyy-MM-dd} to {model.CheckOutDate:yyyy-MM-dd}"),
                        numberOfGuests = model.NumberOfGuests
                    })
                });
            }

            var hotelForBooking = await _hotelService.GetHotelByIdForApiAsync(model.HotelId);
            if (hotelForBooking == null)
            {
                TempData["UserMessage"] = "Hotel not found. Cannot confirm booking.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessGuestName"] = model.GuestFullName;
            TempData["SuccessHotelName"] = model.HotelName;
            TempData["FromSuccess"] = "true";

            return RedirectToAction("BookingSuccess");
        }

        public IActionResult BookingSuccess()
        {
            var model = new BookingSuccessViewModel
            {
                GuestFullName = TempData["SuccessGuestName"] as string,
                HotelName = TempData["SuccessHotelName"] as string
            };
            return View(model);
        }


        private string GetReviewText(decimal score)
        {
            if (score >= 9.0m) return "Superb";
            if (score >= 8.0m) return "Very Good";
            if (score >= 7.0m) return "Good";
            if (score >= 6.0m) return "Pleasant";
            return score <= 0.0m ? "" : "Okay";
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature?.Error, "Unhandled exception at path: {Path}", exceptionHandlerPathFeature?.Path);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Message = "An unexpected internal error occurred.", StatusCode = HttpContext.Response.StatusCode });
        }

        [Route("/Home/HandleError/{statusCode}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HandleError(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var vm = new ErrorViewModel { StatusCode = statusCode, RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, OriginalPath = statusCodeResult?.OriginalPath };
            switch (statusCode)
            {
                case 400: vm.Message = "Bad request."; return View("BadRequest", vm);
                case 401: vm.Message = "Unauthorized."; return View("Unauthorized", vm);
                case 403: vm.Message = "Forbidden."; return View("Forbidden", vm);
                case 404: vm.Message = "Not found."; return View("NotFound", vm);
                default: vm.Message = "An error occurred."; return View("GenericError", vm);
            }
        }
    }
}