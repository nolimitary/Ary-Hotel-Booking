using Aryaans_Hotel_Booking.Models;
using Aryaans_Hotel_Booking.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Linq;

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

        public IActionResult Index()
        {
            return View();
        }
        
        public async Task<IActionResult> SearchResults(
            string? destination, 
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int adults = 1, 
            int children = 0,
            int? minRating = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sortBy = "name",
            string? sortOrder = "asc",
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            string? selectedDatesString = (checkInDate.HasValue && checkOutDate.HasValue) 
                                        ? $"{checkInDate.Value:yyyy-MM-dd} to {checkOutDate.Value:yyyy-MM-dd}" 
                                        : null;
            string? selectedGuestsString = $"Adults: {adults}, Children: {children}";

            if (checkInDate.HasValue && checkInDate.Value < DateTime.Today)
            {
                ModelState.AddModelError("checkInDate", "Check-in date cannot be in the past.");
            }
            if (checkOutDate.HasValue && checkInDate.HasValue && checkOutDate.Value <= checkInDate.Value)
            {
                ModelState.AddModelError("checkOutDate", "Check-out date must be after check-in date.");
            }

            if (!ModelState.IsValid)
            {
                TempData["UserMessage"] = "Invalid search parameters. Please check your dates.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index)); 
            }

            var searchViewModel = await _hotelService.SearchHotelsAsync(
                destination,
                selectedDatesString,
                selectedGuestsString,
                minRating,
                minPrice,
                maxPrice,
                sortBy,
                sortOrder,
                pageNumber,
                pageSize);

            if (searchViewModel == null || !searchViewModel.Results.Any())
            {
                TempData["UserMessage"] = "No hotels found matching your criteria. Try a different search!";
                TempData["MessageType"] = "info";
            }
            
            return View(searchViewModel);
        }

        public async Task<IActionResult> HotelDetails(int id)
        {
            var hotelViewModel = await _hotelService.GetHotelDetailsViewModelAsync(id);
            if (hotelViewModel == null)
            {
                _logger.LogWarning($"Hotel with ID {id} not found for details view.");
                TempData["UserMessage"] = "Sorry, the hotel you are looking for could not be found.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index)); 
            }
            return View(hotelViewModel);
        }

        [Authorize]
        public async Task<IActionResult> Booking(int hotelId, DateTime checkInDate, DateTime checkOutDate, int numberOfGuests)
        {
            var hotelDetailsResult = await _hotelService.GetHotelDetailsViewModelAsync(hotelId);
            if (hotelDetailsResult == null)
            {
                TempData["UserMessage"] = "Hotel details not found. Cannot proceed with booking.";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            if (checkInDate < DateTime.Today || checkOutDate <= checkInDate)
            {
                 TempData["UserMessage"] = "Invalid booking dates selected.";
                 TempData["MessageType"] = "error";
                 return RedirectToAction("HotelDetails", new { id = hotelId });
            }

            var numberOfNights = (checkOutDate - checkInDate).Days;
            if (numberOfNights <= 0) numberOfNights = 1;

            var bookingViewModel = new BookingViewModel
            {
                HotelName = hotelDetailsResult.HotelName,
                ImageUrl = hotelDetailsResult.ImageUrl,
                StarRating = hotelDetailsResult.StarRating,
                LocationName = hotelDetailsResult.LocationName,
                DistanceFromCenter = hotelDetailsResult.DistanceFromCenter,
                ReviewScore = hotelDetailsResult.ReviewScore,
                ReviewScoreText = hotelDetailsResult.ReviewScoreText,
                ReviewCount = hotelDetailsResult.ReviewCount,
                PricePerNight = hotelDetailsResult.PricePerNight,
                CurrencySymbol = hotelDetailsResult.CurrencySymbol,
                NumberOfNights = numberOfNights,
                TotalPrice = numberOfNights * hotelDetailsResult.PricePerNight,
                SelectedDates = $"{checkInDate:MMM dd, yyyy} - {checkOutDate:MMM dd, yyyy}",
                SelectedGuests = $"{numberOfGuests} Guest(s)"
            };

            ViewData["HotelId"] = hotelId;
            ViewData["CheckInDate"] = checkInDate.ToString("yyyy-MM-dd");
            ViewData["CheckOutDate"] = checkOutDate.ToString("yyyy-MM-dd");
            ViewData["NumberOfGuests"] = numberOfGuests;
            
            return View(bookingViewModel);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(CreateBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["UserMessage"] = "There was an error with your booking details. Please review and try again.";
                TempData["MessageType"] = "error";
                
                return RedirectToAction("Booking", new { 
                    hotelId = model.HotelId, 
                    checkInDate = model.CheckInDate, 
                    checkOutDate = model.CheckOutDate, 
                    numberOfGuests = model.NumberOfGuests 
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["UserMessage"] = "You must be logged in to make a booking.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Booking", "Home", new { hotelId = model.HotelId, checkInDate = model.CheckInDate, checkOutDate = model.CheckOutDate, numberOfGuests = model.NumberOfGuests }) });
            }

            bool bookingSuccessful = true; 

            if (bookingSuccessful)
            {
                _logger.LogInformation($"Booking supposedly created for Hotel ID: {model.HotelId}");
                var successViewModel = new BookingSuccessViewModel
                {
                    HotelName = await GetHotelNameByIdFromServiceAsync(model.HotelId),
                };
                
                TempData["SuccessHotelName"] = successViewModel.HotelName;
                TempData["SuccessGuestName"] = successViewModel.GuestFullName;

                return RedirectToAction(nameof(BookingSuccess), successViewModel);
            }
            else
            {
                TempData["UserMessage"] = "We couldn't confirm your booking at this time. Please try again or contact support.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Booking", new { 
                    hotelId = model.HotelId, 
                    checkInDate = model.CheckInDate, 
                    checkOutDate = model.CheckOutDate, 
                    numberOfGuests = model.NumberOfGuests 
                });
            }
        }

        private async Task<string> GetHotelNameByIdFromServiceAsync(int hotelId)
        {
            var hotelDetails = await _hotelService.GetHotelDetailsViewModelAsync(hotelId);
            return hotelDetails?.HotelName ?? "Your Selected Hotel";
        }


        public IActionResult BookingSuccess(BookingSuccessViewModel model)
        {
            if (string.IsNullOrEmpty(model.HotelName) && TempData.ContainsKey("SuccessHotelName"))
            {
                model.HotelName = TempData["SuccessHotelName"] as string;
                model.GuestFullName = TempData["SuccessGuestName"] as string;
            }
            
            if (string.IsNullOrEmpty(model.HotelName))
            {
                ViewData["Title"] = "Booking Confirmation";
            } else {
                ViewData["Title"] = $"Booking Confirmed for {model.HotelName}!";
            }
            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            };

            if (statusCode.HasValue)
            {
                errorViewModel.StatusCode = statusCode.Value; 
                ViewBag.StatusCode = statusCode.Value; 

                if (statusCode == 400) ViewBag.ErrorMessage = "Bad Request.";
                else if (statusCode == 401) ViewBag.ErrorMessage = "Unauthorized.";
                else if (statusCode == 403) ViewBag.ErrorMessage = "Forbidden.";
                else if (statusCode == 404) ViewBag.ErrorMessage = "Sorry, the resource you requested could not be found.";
                else if (statusCode >= 500) ViewBag.ErrorMessage = "An unexpected error occurred on the server.";
            }
            
            if (string.IsNullOrEmpty(ViewBag.ErrorMessage))
            {
                 ViewBag.ErrorMessage = "An unexpected error has occurred.";
            }
            errorViewModel.Message = ViewBag.ErrorMessage;

            return View(errorViewModel);
        }
    }
}
