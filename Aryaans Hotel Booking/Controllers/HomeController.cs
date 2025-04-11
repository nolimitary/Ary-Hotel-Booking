using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Collections.Generic; 

namespace Aryaans_Hotel_Booking.Controllers 
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(string? selectedDates, string? selectedGuests, string? selectedLocation)
        {
            ViewData["SelectedDates"] = selectedDates;
            ViewData["SelectedGuests"] = !string.IsNullOrEmpty(selectedGuests) ? WebUtility.UrlDecode(selectedGuests) : null;
            ViewData["SelectedLocation"] = !string.IsNullOrEmpty(selectedLocation) ? WebUtility.UrlDecode(selectedLocation) : null;
            return View();
        }

        public IActionResult DatePicker()
        {
            var startDate = DateTime.Today;
            int numberOfMonths = 12;
            var viewModel = new DatePickerViewModel { StartDate = startDate, NumberOfMonths = numberOfMonths };
            DateTime currentMonthDate = new DateTime(startDate.Year, startDate.Month, 1);
            for (int i = 0; i < numberOfMonths; i++)
            {
                var monthViewModel = new MonthViewModel { Year = currentMonthDate.Year, Month = currentMonthDate.Month, MonthName = currentMonthDate.ToString("MMMM", CultureInfo.InvariantCulture) };
                DayOfWeek firstDayOfMonth = currentMonthDate.DayOfWeek;
                int placeholders = ((int)firstDayOfMonth - (int)DayOfWeek.Monday + 7) % 7;
                for (int p = 0; p < placeholders; p++) { monthViewModel.Days.Add(new DayViewModel { IsPlaceholder = true }); }
                int daysInMonth = DateTime.DaysInMonth(currentMonthDate.Year, currentMonthDate.Month);
                for (int day = 1; day <= daysInMonth; day++) { var dayDate = new DateTime(currentMonthDate.Year, currentMonthDate.Month, day); monthViewModel.Days.Add(new DayViewModel { DayNumber = day, Date = dayDate, IsToday = (dayDate == DateTime.Today), IsDisabled = (dayDate < DateTime.Today) }); }
                viewModel.Months.Add(monthViewModel);
                currentMonthDate = currentMonthDate.AddMonths(1);
            }
            return View(viewModel);
        }

        public IActionResult GuestPicker()
        {
            return View();
        }

        public IActionResult DestinationPicker()
        {
            return View();
        }

        public IActionResult SearchResults(string destination, string selectedDates, string selectedGuests)
        {
            _logger.LogInformation($"Searching for Destination: {destination}, Dates: {selectedDates}, Guests: {selectedGuests}");
            var dummyResults = new List<HotelResultViewModel>();
            dummyResults.Add(new HotelResultViewModel { HotelName = "Pirin Golf Hotel & Spa", ImageUrl = "/images/pirin-golf.jpg", StarRating = 5, LocationName = "Bansko", DistanceFromCenter = "6.7 km from downtown", ReviewScore = 8.0m, ReviewScoreText = "Very Good", ReviewCount = 716, PricePerNight = 577, CurrencySymbol = "BGN", AvailabilityUrl = "#", RecommendedRooms = new List<RoomInfoViewModel> { new RoomInfoViewModel { RoomTypeName = "Superior Double or Twin", BedInfo = "Multiple bed types" }, new RoomInfoViewModel { RoomTypeName = "Standard Double Room", BedInfo = "1 king bed" } } });
            dummyResults.Add(new HotelResultViewModel { HotelName = "Grand Hotel Bansko", ImageUrl = "/images/grand-bansko.jpg", StarRating = 4, LocationName = "Bansko", DistanceFromCenter = "1.2 km from downtown", ReviewScore = 7.5m, ReviewScoreText = "Good", ReviewCount = 1050, PricePerNight = 350, CurrencySymbol = "BGN", AvailabilityUrl = "#", RecommendedRooms = new List<RoomInfoViewModel> { new RoomInfoViewModel { RoomTypeName = "Deluxe Double", BedInfo = "1 extra-large double bed" } } });
            dummyResults.Add(new HotelResultViewModel { HotelName = "Kempinski Hotel Grand Arena", ImageUrl = "/images/kempinski-bansko.jpg", StarRating = 5, LocationName = "Bansko", DistanceFromCenter = "Directly at Gondola", ReviewScore = 9.1m, ReviewScoreText = "Superb", ReviewCount = 880, PricePerNight = 720, CurrencySymbol = "BGN", AvailabilityUrl = "#", RecommendedRooms = new List<RoomInfoViewModel> { new RoomInfoViewModel { RoomTypeName = "Alpine Superior", BedInfo = "Mountain view, 1 king bed" } } });

            var viewModel = new SearchResultsViewModel
            {
                SearchDestination = WebUtility.UrlDecode(destination ?? "Unknown"),
                SearchDates = WebUtility.UrlDecode(selectedDates ?? "Any"),
                SearchGuests = WebUtility.UrlDecode(selectedGuests ?? "Default"),
                Results = dummyResults
            };


            return View("Results", viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}