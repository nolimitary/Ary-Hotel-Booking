using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Collections.Generic;
using System.Linq; 
using System;      

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




        [HttpGet]
        public IActionResult BookingDetails(string hotelName, string selectedDates)
        {
            string decodedHotelName = WebUtility.UrlDecode(hotelName ?? "");
            string decodedDates = WebUtility.UrlDecode(selectedDates ?? "");

            _logger.LogInformation($"Loading booking details for hotel: {decodedHotelName}, Dates: {decodedDates}");

            HotelResultViewModel? selectedHotel = GetDummyHotelByName(decodedHotelName);

            if (selectedHotel == null)
            {
                _logger.LogWarning($"Hotel not found: {decodedHotelName}");
                return NotFound($"Details for hotel '{decodedHotelName}' not found.");
            }

            int numberOfNights = 1; 
            DateTime checkInDate = DateTime.MinValue;
            DateTime checkOutDate = DateTime.MinValue;
            string dateParseFormat = "yyyy-MM-dd"; 

            if (!string.IsNullOrEmpty(decodedDates) && decodedDates.Contains(" - "))
            {
                string[] dateParts = decodedDates.Split(" - ", StringSplitOptions.TrimEntries);
                if (dateParts.Length == 2 &&
                    DateTime.TryParseExact(dateParts[0], dateParseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out checkInDate) &&
                    DateTime.TryParseExact(dateParts[1], dateParseFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out checkOutDate))
                {
                    if (checkOutDate > checkInDate)
                    {
                        numberOfNights = (int)(checkOutDate - checkInDate).TotalDays;
                    }
                    else
                    {
                        _logger.LogWarning($"Check-out date ({dateParts[1]}) is not after check-in date ({dateParts[0]}). Defaulting to 1 night.");
                    }
                }
                else
                {
                    _logger.LogWarning($"Could not parse date range string: '{decodedDates}'. Expected format '{dateParseFormat} - {dateParseFormat}'. Defaulting to 1 night.");
                }
            }
            else
            {
                _logger.LogWarning($"Selected dates string is empty or invalid format: '{decodedDates}'. Defaulting to 1 night.");
            }


            decimal totalPrice = selectedHotel.PricePerNight * numberOfNights;


            var bookingViewModel = new BookingViewModel
            {
                HotelName = selectedHotel.HotelName,
                ImageUrl = selectedHotel.ImageUrl,
                StarRating = selectedHotel.StarRating,
                LocationName = selectedHotel.LocationName,
                DistanceFromCenter = selectedHotel.DistanceFromCenter,
                ReviewScore = selectedHotel.ReviewScore,
                ReviewScoreText = selectedHotel.ReviewScoreText,
                ReviewCount = selectedHotel.ReviewCount,
                PricePerNight = selectedHotel.PricePerNight,
                CurrencySymbol = selectedHotel.CurrencySymbol,

                NumberOfNights = numberOfNights,
                TotalPrice = totalPrice,
                SelectedDates = decodedDates 
                                            
            };

            return View("BookingDetails", bookingViewModel);
        }

        private HotelResultViewModel? GetDummyHotelByName(string name)
        {
            var allHotels = new List<HotelResultViewModel>();

            allHotels.Add(new HotelResultViewModel { HotelName = "Pirin Golf Hotel & Spa", ImageUrl = "/images/pirin-golf.jpg", StarRating = 5, LocationName = "Bansko", DistanceFromCenter = "6.7 km", ReviewScore = 8.0m, ReviewScoreText = "Very Good", ReviewCount = 716, PricePerNight = 577, CurrencySymbol = "BGN" });
            allHotels.Add(new HotelResultViewModel { HotelName = "Grand Hotel Bansko", ImageUrl = "/images/grand-bansko.jpg", StarRating = 4, LocationName = "Bansko", DistanceFromCenter = "1.2 km", ReviewScore = 7.5m, ReviewScoreText = "Good", ReviewCount = 1050, PricePerNight = 350, CurrencySymbol = "BGN" });
            allHotels.Add(new HotelResultViewModel { HotelName = "Kempinski Hotel Grand Arena", ImageUrl = "/images/kempinski-bansko.jpg", StarRating = 5, LocationName = "Bansko", DistanceFromCenter = "Gondola", ReviewScore = 9.1m, ReviewScoreText = "Superb", ReviewCount = 880, PricePerNight = 720, CurrencySymbol = "BGN" });

            allHotels.Add(new HotelResultViewModel { HotelName = "Premier Inn London Canary Wharf (Westferry)", ImageUrl = "/images/placeholder-hotel-1.jpg", StarRating = 3, LocationName = "Limehouse", DistanceFromCenter = "5km", ReviewScore = 4.4m, ReviewScoreText = "Very Good", ReviewCount = 825, PricePerNight = 150, CurrencySymbol = "£" });
            allHotels.Add(new HotelResultViewModel { HotelName = "The Tower Hotel London", ImageUrl = "/images/placeholder-hotel-2.jpg", StarRating = 4, LocationName = "St Katharine's", DistanceFromCenter = "Near Tower Bridge", ReviewScore = 8.2m, ReviewScoreText = "Very Good", ReviewCount = 14325, PricePerNight = 250, CurrencySymbol = "£" });
            allHotels.Add(new HotelResultViewModel { HotelName = "Nobu Hotel London Portman Square", ImageUrl = "/images/placeholder-hotel-3.jpg", StarRating = 5, LocationName = "Marylebone", DistanceFromCenter = "Central", ReviewScore = 8.4m, ReviewScoreText = "Very Good", ReviewCount = 1646, PricePerNight = 382, CurrencySymbol = "£" });

            return allHotels.FirstOrDefault(h => h.HotelName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IActionResult SearchResults(string destination, string selectedDates, string selectedGuests)
        {
            _logger.LogInformation($"Searching for Destination: {destination}, Dates: {selectedDates}, Guests: {selectedGuests}");

            string decodedGuests = WebUtility.UrlDecode(selectedGuests ?? "");
            int totalGuests = 0;
            if (!string.IsNullOrEmpty(decodedGuests))
            {
                string[] parts = decodedGuests.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    string trimmedPart = part.Trim();
                    string[] numAndType = trimmedPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (numAndType.Length >= 2 && (numAndType[1].Contains("Adult") || numAndType[1].Contains("Kid")))
                    {
                        if (Int32.TryParse(numAndType[0], out int count))
                        {
                            totalGuests += count;
                        }
                    }
                }
            }
            if (totalGuests == 0) totalGuests = 2;

            var dummyResults = new List<HotelResultViewModel>();

            var pirinRooms = new List<(RoomInfoViewModel Room, int Capacity)> {
                (new RoomInfoViewModel { RoomTypeName = "Standard Double Room", BedInfo = "1 king bed" }, 2),
                (new RoomInfoViewModel { RoomTypeName = "Superior Double or Twin", BedInfo = "Multiple bed types" }, 3),
                (new RoomInfoViewModel { RoomTypeName = "Junior Suite", BedInfo = "1 king bed, 1 sofa bed" }, 4)
            };
            dummyResults.Add(new HotelResultViewModel
            {
                HotelName = "Pirin Golf Hotel & Spa",
                ImageUrl = "/pirinGolf.jpeg",
                StarRating = 5,
                LocationName = "Bansko",
                DistanceFromCenter = "6.7 km from downtown",
                ReviewScore = 8.0m,
                ReviewScoreText = "Very Good",
                ReviewCount = 716,
                PricePerNight = 577,
                CurrencySymbol = "BGN",
                AvailabilityUrl = "#",
                RecommendedRooms = pirinRooms.Where(r => r.Capacity >= totalGuests).Select(r => r.Room).ToList()
            });


            var grandBanskoRooms = new List<(RoomInfoViewModel Room, int Capacity)> {
                 (new RoomInfoViewModel { RoomTypeName = "Economy Double", BedInfo = "1 double bed" }, 2),
                 (new RoomInfoViewModel { RoomTypeName = "Deluxe Double", BedInfo = "1 extra-large double bed" }, 2),
                 (new RoomInfoViewModel { RoomTypeName = "Family Room", BedInfo = "Connecting rooms available" }, 4)
            };
            dummyResults.Add(new HotelResultViewModel
            {
                HotelName = "Grand Hotel Bansko",
                ImageUrl = "/grandHotelBansko.jpg",
                StarRating = 4,
                LocationName = "Bansko",
                DistanceFromCenter = "1.2 km from downtown",
                ReviewScore = 7.5m,
                ReviewScoreText = "Good",
                ReviewCount = 1050,
                PricePerNight = 350,
                CurrencySymbol = "BGN",
                AvailabilityUrl = "#",
                RecommendedRooms = grandBanskoRooms.Where(r => r.Capacity >= totalGuests).Select(r => r.Room).ToList()
            });


            var kempinskiRooms = new List<(RoomInfoViewModel Room, int Capacity)> {
                 (new RoomInfoViewModel { RoomTypeName = "Deluxe Room", BedInfo = "1 king bed or 2 twin beds" }, 2),
                 (new RoomInfoViewModel { RoomTypeName = "Alpine Superior", BedInfo = "Mountain view, 1 king bed" }, 2),
                 (new RoomInfoViewModel { RoomTypeName = "Junior Suite", BedInfo = "1 king bed, separate living area" }, 3)
            };
            dummyResults.Add(new HotelResultViewModel
            {
                HotelName = "Kempinski Hotel Grand Arena",
                ImageUrl = "/Kempinski.jpg",
                StarRating = 5,
                LocationName = "Bansko",
                DistanceFromCenter = "Directly at Gondola",
                ReviewScore = 9.1m,
                ReviewScoreText = "Superb",
                ReviewCount = 880,
                PricePerNight = 720,
                CurrencySymbol = "BGN",
                AvailabilityUrl = "#",
                RecommendedRooms = kempinskiRooms.Where(r => r.Capacity >= totalGuests).Select(r => r.Room).ToList()
            });


            var viewModel = new SearchResultsViewModel
            {
                SearchDestination = WebUtility.UrlDecode(destination ?? "Unknown"),
                SearchDates = WebUtility.UrlDecode(selectedDates ?? "Any"),
                SearchGuests = decodedGuests,
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