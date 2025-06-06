using Aryaans_Hotel_Booking.Models;
using Aryaans_Hotel_Booking.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;
using System.Globalization;


namespace Aryaans_Hotel_Booking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHotelService _hotelService;

        public HomeController(ILogger<HomeController> logger, IHotelService hotelService)
        {
            _logger = logger;
            _hotelService = hotelService;
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

            bool success = await _hotelService.AddHotelAsync(model);

            if (success)
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
        public async Task<IActionResult> BookingDetails(string hotelName, string selectedDates)
        {
            var hotelEntity = await _hotelService.GetAllHotelsForApiAsync();
            var targetHotel = hotelEntity.FirstOrDefault(h => h.Name.Equals(WebUtility.UrlDecode(hotelName ?? ""), StringComparison.OrdinalIgnoreCase));

            if (targetHotel == null) return NotFound();
            var vm = new BookingViewModel
            {
                HotelName = targetHotel.Name,
                ImageUrl = targetHotel.ImagePath,
                StarRating = targetHotel.StarRating,
                LocationName = $"{targetHotel.City}, {targetHotel.Country}",
                PricePerNight = targetHotel.PricePerNight,
                SelectedDates = WebUtility.UrlDecode(selectedDates ?? "")
            };
            return View(vm);
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