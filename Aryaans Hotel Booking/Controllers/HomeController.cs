using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Net;

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

        [HttpGet]
        public IActionResult AddHotel()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddHotel(IFormFile Image, string Country, string City, int StarRating, int ReviewCount, decimal ReviewScore, decimal PricePerNight)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(uploadsFolder);

            string fileName = Path.GetRandomFileName() + Path.GetExtension(Image.FileName);
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                Image.CopyTo(stream);
            }

            string virtualPath = $"/images/{fileName}";
            string line = $"{City} Hotel,{Country},{City},{PricePerNight},{StarRating},{ReviewScore},{virtualPath}"; // <--- CHANGED THIS LINE

            string dataPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "hotels.txt");
            System.IO.File.AppendAllLines(dataPath, new[] { line });

            return RedirectToAction("SearchResults", new
            {
                destination = Country,
                selectedDates = "",
                selectedGuests = ""
            });
        }


        public IActionResult BookingDetails(string hotelName, string selectedDates)
        {
            string decodedHotelName = WebUtility.UrlDecode(hotelName ?? "");
            string decodedDates = WebUtility.UrlDecode(selectedDates ?? "");

            var allHotels = GetAllDummyHotels().Concat(LoadAddedHotels()).ToList();
            var selectedHotel = allHotels.FirstOrDefault(h => h.HotelName.Equals(decodedHotelName, StringComparison.OrdinalIgnoreCase));
            if (selectedHotel == null)
                return NotFound($"Hotel '{decodedHotelName}' not found.");

            DateTime checkIn = DateTime.MinValue, checkOut = DateTime.MinValue;
            int nights = 1;

            if (!string.IsNullOrEmpty(decodedDates) && decodedDates.Contains(" - "))
            {
                var parts = decodedDates.Split(" - ");
                DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkIn);
                DateTime.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkOut);
                if (checkOut > checkIn) nights = (int)(checkOut - checkIn).TotalDays;
            }

            return View("BookingDetails", new BookingViewModel
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
                NumberOfNights = nights,
                TotalPrice = selectedHotel.PricePerNight * nights,
                SelectedDates = decodedDates
            });
        }

        public IActionResult SearchResults(string destination, string selectedDates, string selectedGuests)
        {
            string decodedGuests = WebUtility.UrlDecode(selectedGuests ?? "");
            int totalGuests = ParseGuestCount(decodedGuests);

            var dummyResults = GetAllDummyHotels();
            var addedHotels = LoadAddedHotels();
            dummyResults.AddRange(addedHotels);

            var viewModel = new SearchResultsViewModel
            {
                SearchDestination = WebUtility.UrlDecode(destination ?? "Unknown"),
                SearchDates = WebUtility.UrlDecode(selectedDates ?? "Any"),
                SearchGuests = decodedGuests,
                Results = dummyResults
            };

            return View("Results", viewModel);
        }

        private int ParseGuestCount(string guestText)
        {
            int count = 0;
            foreach (var part in guestText.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var words = part.Trim().Split(' ');
                if (words.Length >= 2 && int.TryParse(words[0], out int n))
                {
                    count += n;
                }
            }
            return count == 0 ? 2 : count;
        }

        private List<HotelResultViewModel> LoadAddedHotels()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "hotels.txt");
            var results = new List<HotelResultViewModel>();

            if (!System.IO.File.Exists(path)) return results;

            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue; 

                var parts = line.Split(','); 
                if (parts.Length >= 7)      
                {
                    bool priceParsed = decimal.TryParse(parts[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
                    bool starsParsed = int.TryParse(parts[4].Trim(), out var stars);
                    bool scoreParsed = decimal.TryParse(parts[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var score);

                    results.Add(new HotelResultViewModel
                    {
                        HotelName = parts[0].Trim(), 
                        LocationName = $"{parts[2].Trim()}, {parts[1].Trim()}",
                        StarRating = starsParsed ? stars : 0,                    
                        ReviewCount = 0, 
                        ReviewScore = scoreParsed ? score : 0m,                   
                        ReviewScoreText = GetReviewText(scoreParsed ? score : 0m),
                        PricePerNight = priceParsed ? price : 0m,                 
                        ImageUrl = parts[6].Trim(),                              
                        CurrencySymbol = "BGN" 
                    });
                }
                else
                {
                    _logger.LogWarning("Skipping malformed line in hotels.txt (not enough parts after splitting by comma): {Line}", line);
                }
            }
            return results;
        }


        private string GetReviewText(decimal score)
        {
            if (score >= 9) return "Superb";
            if (score >= 8) return "Very Good";
            if (score >= 6) return "Good";
            return "Okay";
        }

        private List<HotelResultViewModel> GetAllDummyHotels()
        {
            return new List<HotelResultViewModel>
            {
                new() {
                    HotelName = "Pirin Golf Hotel & Spa",
                    ImageUrl = "/pirinGolf.jpeg",
                    StarRating = 5,
                    LocationName = "Bansko",
                    DistanceFromCenter = "6.7 km from downtown",
                    ReviewScore = 8.0m,
                    ReviewScoreText = "Very Good",
                    ReviewCount = 716,
                    PricePerNight = 577,
                    CurrencySymbol = "BGN"
                },
                new() {
                    HotelName = "Grand Hotel Bansko",
                    ImageUrl = "/grandHotelBansko.jpg",
                    StarRating = 4,
                    LocationName = "Bansko",
                    DistanceFromCenter = "1.2 km from downtown",
                    ReviewScore = 7.5m,
                    ReviewScoreText = "Good",
                    ReviewCount = 1050,
                    PricePerNight = 350,
                    CurrencySymbol = "BGN"
                },
                new() {
                    HotelName = "Kempinski Hotel Grand Arena",
                    ImageUrl = "/Kempinski.jpg",
                    StarRating = 5,
                    LocationName = "Bansko",
                    DistanceFromCenter = "Directly at Gondola",
                    ReviewScore = 9.1m,
                    ReviewScoreText = "Superb",
                    ReviewCount = 880,
                    PricePerNight = 720,
                    CurrencySymbol = "BGN"
                }
            };
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
