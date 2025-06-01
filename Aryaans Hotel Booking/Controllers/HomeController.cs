using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Aryaans_Hotel_Booking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string? selectedDates, string? selectedGuests, string? selectedLocation)
        {
            ViewData["SelectedDates"] = selectedDates;
            ViewData["SelectedGuests"] = !string.IsNullOrEmpty(selectedGuests) ? WebUtility.UrlDecode(selectedGuests) : null;
            ViewData["SelectedLocation"] = !string.IsNullOrEmpty(selectedLocation) ? WebUtility.UrlDecode(selectedLocation) : null;

            var featuredHotelsFromDb = await _context.Hotels
                                                 .OrderByDescending(h => h.StarRating) 
                                                 .ThenByDescending(h => h.ReviewScore)
                                                 .Take(3) 
                                                 .ToListAsync();

            var featuredHotelViewModels = new List<HotelResultViewModel>();
            foreach (var hotel in featuredHotelsFromDb)
            {
                featuredHotelViewModels.Add(new HotelResultViewModel
                {
                    Id = hotel.Id,
                    HotelName = hotel.Name,
                    ImageUrl = hotel.ImagePath, 
                    StarRating = hotel.StarRating,
                    LocationName = $"{hotel.City}, {hotel.Country}",
                    ReviewScore = (decimal)(hotel.ReviewScore ?? 0.0),
                    ReviewScoreText = GetReviewText((decimal)(hotel.ReviewScore ?? 0.0)),
                    PricePerNight = hotel.PricePerNight,
                    CurrencySymbol = "BGN" 
                });
            }
            return View(featuredHotelViewModels);
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
            // Check if the user is an admin.
            // The actual session key might be different based on your login implementation.
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                _logger.LogWarning("Non-admin user tried to access AddHotel GET page.");
                // Return a 403 Forbidden status if the user is not authorized.
                return Forbid();
            }

            // Return the view for adding a hotel.
            // If you have a specific ViewModel for the AddHotel page (e.g., AddHotelViewModel),
            // you might want to initialize and pass it here:
            // return View(new AddHotelViewModel());
            return View();
        }

        // POST: Home/AddHotel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHotel(
            IFormFile? Image, // Image can be nullable if it's optional
            string Name,
            string Country,
            string City,
            string Address,
            string Description,
            int StarRating,
            decimal ReviewScore,
            decimal PricePerNight)
        {
            // Authorization check
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                _logger.LogWarning("Non-admin user tried to POST to AddHotel.");
                return Forbid();
            }

            // Create an object to hold the form data, useful for repopulating the form if validation fails.
            // This could be your Hotel entity or a specific AddHotelViewModel.
            // Using Hotel entity here for simplicity, assuming the view can bind to its properties.
            var hotelViewModelForFormRepopulation = new Hotel
            {
                Name = Name,
                Country = Country,
                City = City,
                Address = Address,
                Description = Description,
                PricePerNight = PricePerNight,
                StarRating = StarRating,
                ReviewScore = (double)ReviewScore
                // ImagePath will be set if an image is uploaded or if it's part of a more complex ViewModel
            };

            // Server-side validation based on model annotations (e.g., [Required])
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddHotel POST request failed model validation.");
                foreach (var entry in ModelState)
                {
                    if (entry.Value.Errors.Any())
                    {
                        _logger.LogWarning($"Field: {entry.Key}");
                        foreach (var error in entry.Value.Errors)
                        {
                            _logger.LogWarning($"- Error: {error.ErrorMessage}");
                        }
                    }
                }
                // Return the view with the submitted data to repopulate the form and show validation errors.
                return View(hotelViewModelForFormRepopulation);
            }

            string? virtualImagePath = null; // Path to be stored in the database (URL path)

            // Image processing logic
            if (Image != null && Image.Length > 0)
            {
                // Define the server-side folder path to save images.
                // Path.Combine correctly uses backslashes on Windows for the physical path.
                string serverUploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels");

                // Ensure the directory exists; create it if it doesn't.
                Directory.CreateDirectory(serverUploadsFolder);

                // Generate a unique file name to prevent overwriting existing files.
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Image.FileName);

                // Full physical path to the file on the server.
                string serverFilePath = Path.Combine(serverUploadsFolder, uniqueFileName);

                try
                {
                    // Save the uploaded image to the server.
                    using (var fileStream = new FileStream(serverFilePath, FileMode.Create))
                    {
                        await Image.CopyToAsync(fileStream);
                    }
                    // Set the virtual path (URL path) for database storage and web access.
                    // This path uses forward slashes, which is standard for URLs.
                    virtualImagePath = $"/images/hotels/{uniqueFileName}";
                    _logger.LogInformation($"Image '{uniqueFileName}' uploaded successfully to '{serverFilePath}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error uploading image '{Image.FileName}'.");
                    ModelState.AddModelError("Image", "Error uploading image. Please try again.");
                    // Return the view with the model to repopulate form fields and show the error.
                    return View(hotelViewModelForFormRepopulation);
                }
            }

            // Create the Hotel entity to be saved to the database.
            var hotel = new Hotel
            {
                Name = Name,
                Country = Country,
                City = City,
                Address = Address,
                Description = Description,
                PricePerNight = PricePerNight,
                StarRating = StarRating,
                ReviewScore = (double)ReviewScore,
                ImagePath = virtualImagePath // This can be null if no image was uploaded
            };

            try
            {
                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel '{hotel.Name}' (ID: {hotel.Id}) added to database by user {HttpContext.Session.GetString("Username")}.");
                TempData["SuccessMessage"] = $"Hotel '{hotel.Name}' added successfully!";
            }
            catch (DbUpdateException ex) 
            {
                _logger.LogError(ex, $"Database error saving hotel '{hotel.Name}'. InnerException: {ex.InnerException?.Message}");
                TempData["ErrorMessage"] = "Error saving hotel to database. Please check the data and try again. If the problem persists, contact support.";
                return View(hotel);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Generic error saving hotel '{hotel.Name}'.");
                TempData["ErrorMessage"] = "An unexpected error occurred while saving the hotel. Please try again.";
                return View(hotel);
            }

            return RedirectToAction("SearchResults", new { destination = Country });
        }


        // GET: Home/EditHotel/5
        [HttpGet]
        public async Task<IActionResult> EditHotel(int? id)
        {
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                _logger.LogWarning("Non-admin user tried to access EditHotel GET page.");
                return Forbid();
            }

            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                return NotFound();
            }

            var viewModel = new EditHotelViewModel
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Country = hotel.Country,
                City = hotel.City,
                StarRating = hotel.StarRating,
                PricePerNight = hotel.PricePerNight,
                ReviewScore = (decimal)(hotel.ReviewScore ?? 0.0),
                ExistingImagePath = hotel.ImagePath
            };

            return View("EditHotel", viewModel); // We'll create EditHotel.cshtml
        }

        // POST: Home/EditHotel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHotel(int id, EditHotelViewModel viewModel, IFormFile? Image)
        {
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                _logger.LogWarning("Non-admin user tried to POST to EditHotel.");
                return Forbid();
            }

            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var hotelToUpdate = await _context.Hotels.FindAsync(id);
                if (hotelToUpdate == null)
                {
                    return NotFound();
                }

                hotelToUpdate.Name = viewModel.Name;
                hotelToUpdate.Country = viewModel.Country;
                hotelToUpdate.City = viewModel.City;
                hotelToUpdate.StarRating = viewModel.StarRating;
                hotelToUpdate.PricePerNight = viewModel.PricePerNight;
                hotelToUpdate.ReviewScore = (double)viewModel.ReviewScore;

                if (Image != null && Image.Length > 0)
                {
                    // Delete old image if it exists and is not a default placeholder
                    if (!string.IsNullOrEmpty(hotelToUpdate.ImagePath))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, hotelToUpdate.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePath);
                                _logger.LogInformation($"Old image '{oldImagePath}' deleted.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error deleting old image '{oldImagePath}'.");
                                // Log error but continue, as replacing image is main goal
                            }
                        }
                    }

                    // Save new image
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Image.FileName);
                    string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var stream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await Image.CopyToAsync(stream);
                        }
                        hotelToUpdate.ImagePath = $"/images/hotels/{uniqueFileName}";
                        _logger.LogInformation($"New image '{uniqueFileName}' uploaded for hotel ID {hotelToUpdate.Id}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error uploading new image for hotel ID {hotelToUpdate.Id}.");
                        ModelState.AddModelError("Image", "Error uploading new image. Please try again.");
                        viewModel.ExistingImagePath = hotelToUpdate.ImagePath; // keep showing old image path if new upload fails
                        return View("EditHotel", viewModel);
                    }
                }
                // If no new image is uploaded, hotelToUpdate.ImagePath remains unchanged.

                try
                {
                    _context.Update(hotelToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Hotel '{hotelToUpdate.Name}' updated successfully!";
                    _logger.LogInformation($"Hotel ID {hotelToUpdate.Id} ('{hotelToUpdate.Name}') updated by user {HttpContext.Session.GetString("Username")}.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelExists(hotelToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError($"Concurrency error updating hotel ID {hotelToUpdate.Id}.");
                        TempData["ErrorMessage"] = "Error updating hotel due to a concurrency issue. Please try again.";
                        throw; // Re-throw for developer exception page or global handler
                    }
                }
                return RedirectToAction(nameof(SearchResults), new { destination = hotelToUpdate.Country });
            }
            // If ModelState is invalid, return to view with errors and existing image path
            if (string.IsNullOrEmpty(viewModel.ExistingImagePath) && id > 0)
            {
                var existingHotel = await _context.Hotels.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
                if (existingHotel != null) viewModel.ExistingImagePath = existingHotel.ImagePath;
            }
            return View("EditHotel", viewModel);
        }


        // POST: Home/DeleteHotel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                _logger.LogWarning("Non-admin user tried to POST to DeleteHotel.");
                return Forbid();
            }

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                TempData["ErrorMessage"] = "Hotel not found.";
                return RedirectToAction(nameof(SearchResults));
            }

            string? imagePathToDelete = hotel.ImagePath; // Store before deleting hotel object

            try
            {
                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Hotel '{hotel.Name}' deleted successfully.";
                _logger.LogInformation($"Hotel ID {id} ('{hotel.Name}') deleted by user {HttpContext.Session.GetString("Username")}.");

                // Delete image file after successful DB deletion
                if (!string.IsNullOrEmpty(imagePathToDelete))
                {
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePathToDelete.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        try
                        {
                            System.IO.File.Delete(fullPath);
                            _logger.LogInformation($"Associated image '{fullPath}' deleted.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error deleting image file '{fullPath}' for deleted hotel ID {id}.");
                            TempData["WarningMessage"] = $"Hotel deleted, but couldn't remove image file: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting hotel ID {id}.");
                TempData["ErrorMessage"] = $"Error deleting hotel: {ex.Message}";
            }

            return RedirectToAction(nameof(SearchResults), new { destination = hotel.Country }); // Redirect to search results, possibly for the same country
        }


        public async Task<IActionResult> BookingDetails(string hotelName, string selectedDates)
        {
            string decodedHotelName = WebUtility.UrlDecode(hotelName ?? "");
            string decodedDates = WebUtility.UrlDecode(selectedDates ?? "");

            var hotelEntity = await _context.Hotels
                                     .FirstOrDefaultAsync(h => h.Name.ToLower() == decodedHotelName.ToLower());

            if (hotelEntity == null)
            {
                _logger.LogWarning($"BookingDetails: Hotel '{decodedHotelName}' not found in database.");
                return NotFound($"Hotel '{decodedHotelName}' not found.");
            }

            var selectedHotelViewModel = new HotelResultViewModel
            {
                Id = hotelEntity.Id, // Populate Id
                HotelName = hotelEntity.Name,
                ImageUrl = hotelEntity.ImagePath,
                StarRating = hotelEntity.StarRating,
                LocationName = $"{hotelEntity.City}, {hotelEntity.Country}",
                DistanceFromCenter = "N/A",
                ReviewScore = (decimal)(hotelEntity.ReviewScore ?? 0.0),
                ReviewScoreText = GetReviewText((decimal)(hotelEntity.ReviewScore ?? 0.0)),
                ReviewCount = 0,
                PricePerNight = hotelEntity.PricePerNight,
                CurrencySymbol = "BGN"
            };

            DateTime checkIn = DateTime.MinValue, checkOut = DateTime.MinValue;
            int nights = 1;

            if (!string.IsNullOrEmpty(decodedDates) && decodedDates.Contains(" - "))
            {
                var parts = decodedDates.Split(" - ");
                if (parts.Length == 2)
                {
                    DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkIn);
                    DateTime.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkOut);
                    if (checkOut > checkIn)
                    {
                        nights = (int)(checkOut - checkIn).TotalDays;
                    }
                    else
                    {
                        _logger.LogWarning($"BookingDetails: Check-out date '{parts[1]}' is not after check-in date '{parts[0]}'. Defaulting to 1 night.");
                        nights = 1;
                    }
                }
                else
                {
                    _logger.LogWarning($"BookingDetails: Selected dates '{decodedDates}' format is invalid. Defaulting to 1 night.");
                }
            }
            else
            {
                _logger.LogInformation("BookingDetails: No valid selected dates provided. Defaulting to 1 night.");
            }

            return View("BookingDetails", new BookingViewModel
            {
                HotelName = selectedHotelViewModel.HotelName,
                ImageUrl = selectedHotelViewModel.ImageUrl,
                StarRating = selectedHotelViewModel.StarRating,
                LocationName = selectedHotelViewModel.LocationName,
                DistanceFromCenter = selectedHotelViewModel.DistanceFromCenter,
                ReviewScore = selectedHotelViewModel.ReviewScore,
                ReviewScoreText = selectedHotelViewModel.ReviewScoreText,
                ReviewCount = selectedHotelViewModel.ReviewCount,
                PricePerNight = selectedHotelViewModel.PricePerNight,
                CurrencySymbol = selectedHotelViewModel.CurrencySymbol,
                NumberOfNights = nights,
                TotalPrice = selectedHotelViewModel.PricePerNight * nights,
                SelectedDates = decodedDates
            });
        }

        public async Task<IActionResult> SearchResults(string destination, string selectedDates, string selectedGuests)
        {
            string decodedDestination = WebUtility.UrlDecode(destination ?? "");
            string decodedGuests = WebUtility.UrlDecode(selectedGuests ?? "");

            var hotelResults = new List<HotelResultViewModel>();
            List<Hotel> hotelsFromDb;

            if (!string.IsNullOrEmpty(decodedDestination))
            {
                hotelsFromDb = await _context.Hotels
                                          .Where(h => h.Country.ToLower() == decodedDestination.ToLower())
                                          .ToListAsync();
                _logger.LogInformation($"Found {hotelsFromDb.Count} hotels in database for country '{decodedDestination}'.");
            }
            else
            {
                hotelsFromDb = await _context.Hotels.ToListAsync();
                _logger.LogInformation($"No specific destination provided. Displaying {hotelsFromDb.Count} hotels from database.");
            }

            foreach (var hotel in hotelsFromDb)
            {
                hotelResults.Add(new HotelResultViewModel
                {
                    Id = hotel.Id, 
                    HotelName = hotel.Name,
                    ImageUrl = hotel.ImagePath,
                    StarRating = hotel.StarRating,
                    LocationName = $"{hotel.City}, {hotel.Country}",
                    DistanceFromCenter = "N/A",
                    ReviewScore = (decimal)(hotel.ReviewScore ?? 0.0),
                    ReviewScoreText = GetReviewText((decimal)(hotel.ReviewScore ?? 0.0)),
                    ReviewCount = 0,
                    PricePerNight = hotel.PricePerNight,
                    CurrencySymbol = "BGN"
                });
            }

            var viewModel = new SearchResultsViewModel
            {
                SearchDestination = decodedDestination,
                SearchDates = WebUtility.UrlDecode(selectedDates ?? "Any"),
                SearchGuests = decodedGuests,
                Results = hotelResults
            };

            return View("Results", viewModel);
        }

        private int ParseGuestCount(string guestText)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(guestText))
            {
                foreach (var part in guestText.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var words = part.Trim().Split(' ');
                    if (words.Length >= 2 && int.TryParse(words[0], out int n))
                    {
                        count += n;
                    }
                }
            }
            return count == 0 ? 1 : count;
        }

        private string GetReviewText(decimal score)
        {
            if (score >= 9.0m) return "Superb";
            if (score >= 8.0m) return "Very Good";
            if (score >= 7.0m) return "Good";
            if (score >= 6.0m) return "Pleasant";
            return "Okay";
        }

        private bool HotelExists(int id)
        {
            return _context.Hotels.Any(e => e.Id == id);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteHotel(int? id)
        {
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to delete hotels.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null)
            {
                _logger.LogWarning("DeleteHotel GET called with null ID.");
                TempData["ErrorMessage"] = "Hotel ID not provided.";
                return NotFound("Hotel ID not provided.");
            }

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(m => m.Id == id);

            if (hotel == null)
            {
                _logger.LogWarning($"DeleteHotel GET: Hotel with ID {id} not found.");
                TempData["ErrorMessage"] = $"Hotel with ID {id} not found.";
                return NotFound($"Hotel with ID {id} not found.");
            }

            _logger.LogInformation($"Displaying delete confirmation for Hotel ID {id}, Name: {hotel.Name}.");
            return View(hotel);
        }

        [HttpPost, ActionName("DeleteHotel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHotelConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Username") != "admin")
            {
                TempData["ErrorMessage"] = "You are not authorized to delete hotels.";
                _logger.LogWarning("Unauthorized attempt to POST DeleteHotelConfirmed.");
                return RedirectToAction(nameof(Index));
            }

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                _logger.LogWarning($"DeleteHotel POST: Hotel with ID {id} not found for deletion.");
                TempData["ErrorMessage"] = $"Hotel with ID {id} not found.";
                return NotFound($"Hotel with ID {id} not found.");
            }

            try
            {
                var bookingsExist = await _context.Bookings.AnyAsync(b => b.HotelId == id);
                if (bookingsExist)
                {
                    _logger.LogWarning($"Attempt to delete Hotel ID {id} which has existing bookings.");
                    TempData["ErrorMessage"] = "Cannot delete this hotel as it has existing bookings. Please remove associated bookings first.";
                    return RedirectToAction(nameof(DeleteHotel), new { id = id });
                }

                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel ID {id}, Name: {hotel.Name} deleted successfully.");
                TempData["SuccessMessage"] = $"Hotel '{hotel.Name}' was successfully deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error deleting Hotel ID {id}, Name: {hotel.Name}. It might be in use.");
                TempData["ErrorMessage"] = $"Error deleting hotel '{hotel.Name}'. It might be referenced by other records.";
                return View("DeleteHotel", hotel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error deleting Hotel ID {id}, Name: {hotel.Name}.");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the hotel.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
