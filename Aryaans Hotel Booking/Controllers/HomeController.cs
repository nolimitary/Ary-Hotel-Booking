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
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;


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


        [Route("/Home/HandleError/{statusCode}")] 
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HandleError(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            var viewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode,
                OriginalPath = statusCodeResult?.OriginalPath 
            };

            switch (statusCode)
            {
                case 400:
                    ViewData["Title"] = "Bad Request";
                    viewModel.Message = "Oops! It seems like your request was a bit jumbled. Our server couldn't understand it.";
                    return View("BadRequest", viewModel);
                case 401:
                    ViewData["Title"] = "Unauthorized";
                    viewModel.Message = "Whoops! You need to be logged in to see this page. It's a secret handshake kind of deal.";
                    return View("Unauthorized", viewModel);
                case 403:
                    ViewData["Title"] = "Forbidden";
                    viewModel.Message = "Access Denied! It looks like you don't have the secret key for this door.";
                    return View("Forbidden", viewModel);
                case 404:
                    ViewData["Title"] = "Page Not Found";
                    viewModel.Message = "Oh no! It looks like this page went on an adventure and got lost.";
                    return View("NotFound", viewModel);
                default:
                    ViewData["Title"] = $"Error {statusCode}";
                    viewModel.Message = $"Hmm, something unexpected happened (Error {statusCode}). We're not quite sure what!";
                    return View("GenericError", viewModel);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() 
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature?.Error, "Unhandled exception at path: {Path}", exceptionHandlerPathFeature?.Path);

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = "An unexpected internal error occurred. We're looking into it!",
                StatusCode = HttpContext.Response.StatusCode 
            });
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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ListRooms(int hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null)
            {
                TempData["ErrorMessage"] = "Hotel not found.";
                return RedirectToAction(nameof(Index));
            }

            var rooms = await _context.Rooms
                                .Where(r => r.HotelId == hotelId)
                                .Select(r => new RoomViewModel
                                {
                                    Id = r.Id,
                                    RoomNumber = r.RoomNumber,
                                    RoomType = r.RoomType,
                                    PricePerNight = r.PricePerNight,
                                    Capacity = r.Capacity,
                                    IsAvailable = r.IsAvailable,
                                    Amenities = r.Amenities
                                })
                                .ToListAsync();

            var viewModel = new ListRoomsViewModel
            {
                HotelId = hotelId,
                HotelName = hotel.Name,
                Rooms = rooms
            };
            _logger.LogInformation($"Admin listing rooms for Hotel ID: {hotelId} ({hotel.Name}). Found {rooms.Count} rooms.");
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AddRoom(int hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null)
            {
                TempData["ErrorMessage"] = "Hotel not found. Cannot add room.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new AddRoomViewModel
            {
                HotelId = hotelId,
                HotelName = hotel.Name,
                IsAvailable = true
            };
            _logger.LogInformation($"Admin accessing AddRoom GET page for Hotel ID: {hotelId} ({hotel.Name}).");
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoom(AddRoomViewModel model)
        {
            var hotel = await _context.Hotels.FindAsync(model.HotelId);
            if (hotel == null)
            {
                ModelState.AddModelError("HotelId", "Associated hotel not found.");

                model.HotelName = "Unknown Hotel";
            }
            else
            {
                model.HotelName = hotel.Name;
            }


            if (ModelState.IsValid)
            {
                var room = new Room
                {
                    HotelId = model.HotelId,
                    RoomNumber = model.RoomNumber,
                    RoomType = model.RoomType,
                    Description = model.Description,
                    PricePerNight = model.PricePerNight,
                    Capacity = model.Capacity,
                    IsAvailable = model.IsAvailable,
                    Amenities = model.Amenities
                };

                try
                {
                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Admin successfully added Room Number: {room.RoomNumber} to Hotel ID: {room.HotelId}. New Room ID: {room.Id}");
                    TempData["SuccessMessage"] = $"Room '{room.RoomNumber} - {room.RoomType}' added successfully to {hotel?.Name ?? "the hotel"}.";
                    return RedirectToAction(nameof(ListRooms), new { hotelId = model.HotelId });
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, $"Database error adding room to Hotel ID: {model.HotelId}.");
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Generic error adding room to Hotel ID: {model.HotelId}.");
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }

            _logger.LogWarning($"Admin AddRoom POST failed for Hotel ID: {model.HotelId}. ModelState is invalid.");
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditRoom(int roomId)
        {
            var room = await _context.Rooms
                                   .Include(r => r.Hotel)
                                   .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Hotel == null)
            {
                TempData["ErrorMessage"] = "Room not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new EditRoomViewModel
            {
                Id = room.Id,
                HotelId = room.HotelId,
                HotelName = room.Hotel.Name,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                Description = room.Description,
                PricePerNight = room.PricePerNight,
                Capacity = room.Capacity,
                IsAvailable = room.IsAvailable,
                Amenities = room.Amenities
            };
            _logger.LogInformation($"Admin loading EditRoom page for Room ID: {roomId} in Hotel: {room.Hotel.Name}.");
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(EditRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Admin EditRoom POST for Room ID: {model.Id} failed ModelState validation.");
                var hotelForName = await _context.Hotels.FindAsync(model.HotelId);
                model.HotelName = hotelForName?.Name ?? "Unknown Hotel";
                return View(model);
            }

            var roomToUpdate = await _context.Rooms.FindAsync(model.Id);

            if (roomToUpdate == null)
            {
                TempData["ErrorMessage"] = "Room not found for update.";
                _logger.LogWarning($"Admin EditRoom POST: Room with ID {model.Id} not found.");
                return RedirectToAction(nameof(Index));
            }

            if (roomToUpdate.HotelId != model.HotelId)
            {
                _logger.LogError($"Security Alert: Admin EditRoom POST attempt to change HotelId for Room ID: {model.Id}.");
                ModelState.AddModelError("", "Cannot change the associated hotel of a room.");
                var hotelForName = await _context.Hotels.FindAsync(model.HotelId);
                model.HotelName = hotelForName?.Name ?? "Unknown Hotel";
                return View(model);
            }


            roomToUpdate.RoomNumber = model.RoomNumber;
            roomToUpdate.RoomType = model.RoomType;
            roomToUpdate.Description = model.Description;
            roomToUpdate.PricePerNight = model.PricePerNight;
            roomToUpdate.Capacity = model.Capacity;
            roomToUpdate.IsAvailable = model.IsAvailable;
            roomToUpdate.Amenities = model.Amenities;

            try
            {
                _context.Update(roomToUpdate);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Admin successfully updated Room ID: {roomToUpdate.Id} for Hotel ID: {roomToUpdate.HotelId}.");
                TempData["SuccessMessage"] = $"Room '{roomToUpdate.RoomNumber} - {roomToUpdate.RoomType}' updated successfully.";
                return RedirectToAction(nameof(ListRooms), new { hotelId = roomToUpdate.HotelId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency error updating Room ID: {model.Id}.");
                ModelState.AddModelError("", "The room was modified by another user. Please reload and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Room ID: {model.Id}.");
                ModelState.AddModelError("", "An unexpected error occurred while updating the room.");
            }

            var hotelOriginal = await _context.Hotels.FindAsync(model.HotelId);
            model.HotelName = hotelOriginal?.Name ?? "Unknown Hotel";
            return View(model);
        }

        public IActionResult GuestPicker() => View();

        public IActionResult DestinationPicker() => View();

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AddHotel()
        {
            _logger.LogInformation("Admin user accessing AddHotel GET page.");
            return View(new AddHotelViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddHotel(AddHotelViewModel model)
        {
            _logger.LogInformation($"Admin user attempting to POST to AddHotel with hotel name: {model.Name}");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddHotel POST request failed model validation.");
                return View(model);
            }

            string? virtualImagePath = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                string serverUploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels");
                Directory.CreateDirectory(serverUploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Image.FileName);
                string serverFilePath = Path.Combine(serverUploadsFolder, uniqueFileName);

                try
                {
                    using (var fileStream = new FileStream(serverFilePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fileStream);
                    }
                    virtualImagePath = $"/images/hotels/{uniqueFileName}";
                    _logger.LogInformation($"Image '{uniqueFileName}' uploaded successfully to '{serverFilePath}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error uploading image '{model.Image.FileName}'.");
                    ModelState.AddModelError("Image", "Error uploading image. Please try again.");
                    return View(model);
                }
            }

            var hotel = new Hotel
            {
                Name = model.Name,
                Country = model.Country,
                City = model.City,
                Address = model.Address,
                Description = model.Description,
                PricePerNight = model.PricePerNight,
                StarRating = model.StarRating,
                ReviewScore = (double)model.ReviewScore,
                ImagePath = virtualImagePath
            };

            try
            {
                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel '{hotel.Name}' (ID: {hotel.Id}) added to database by admin user.");
                TempData["SuccessMessage"] = $"Hotel '{hotel.Name}' added successfully!";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database error saving hotel '{hotel.Name}'. InnerException: {ex.InnerException?.Message}");
                TempData["ErrorMessage"] = "Error saving hotel to database. Please check the data and try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Generic error saving hotel '{hotel.Name}'.");
                TempData["ErrorMessage"] = "An unexpected error occurred while saving the hotel. Please try again.";
                return View(model);
            }

            return RedirectToAction("SearchResults", new { destination = hotel.Country });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditHotel(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("EditHotel GET called with null ID by admin.");
                return NotFound();
            }

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                _logger.LogWarning($"EditHotel GET: Hotel with ID {id} not found for admin edit.");
                return NotFound();
            }
            _logger.LogInformation($"Admin loading EditHotel page for Hotel ID {id}, Name: {hotel.Name}.");

            var viewModel = new EditHotelViewModel
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Country = hotel.Country,
                City = hotel.City,
                Address = hotel.Address,
                Description = hotel.Description,
                StarRating = hotel.StarRating,
                PricePerNight = hotel.PricePerNight,
                ReviewScore = (decimal)(hotel.ReviewScore ?? 0.0),
                ExistingImagePath = hotel.ImagePath
            };
            return View("EditHotel", viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHotel(int id, EditHotelViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                _logger.LogWarning($"EditHotel POST ID mismatch. Route ID: {id}, ViewModel ID: {viewModel.Id}.");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var hotelToUpdate = await _context.Hotels.FindAsync(id);
                if (hotelToUpdate == null)
                {
                    _logger.LogWarning($"EditHotel POST: Hotel with ID {id} not found for update.");
                    return NotFound();
                }

                hotelToUpdate.Name = viewModel.Name;
                hotelToUpdate.Country = viewModel.Country;
                hotelToUpdate.City = viewModel.City;
                hotelToUpdate.Address = viewModel.Address;
                hotelToUpdate.Description = viewModel.Description;
                hotelToUpdate.StarRating = viewModel.StarRating;
                hotelToUpdate.PricePerNight = viewModel.PricePerNight;
                hotelToUpdate.ReviewScore = (double?)viewModel.ReviewScore;

                if (viewModel.NewImage != null && viewModel.NewImage.Length > 0)
                {
                    if (!string.IsNullOrEmpty(hotelToUpdate.ImagePath))
                    {
                        string oldImagePathOnServer = Path.Combine(_webHostEnvironment.WebRootPath, hotelToUpdate.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePathOnServer))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePathOnServer);
                                _logger.LogInformation($"Old image '{oldImagePathOnServer}' deleted.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error deleting old image '{oldImagePathOnServer}'.");
                            }
                        }
                    }
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(viewModel.NewImage.FileName);
                    string newFilePathOnServer = Path.Combine(uploadsFolder, uniqueFileName);
                    try
                    {
                        using (var stream = new FileStream(newFilePathOnServer, FileMode.Create))
                        {
                            await viewModel.NewImage.CopyToAsync(stream);
                        }
                        hotelToUpdate.ImagePath = $"/images/hotels/{uniqueFileName}";
                        _logger.LogInformation($"New image '{uniqueFileName}' uploaded for hotel ID {hotelToUpdate.Id}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error uploading new image for hotel ID {hotelToUpdate.Id}.");
                        ModelState.AddModelError("NewImage", "Error uploading new image. Please try again.");
                        viewModel.ExistingImagePath = hotelToUpdate.ImagePath;
                        return View("EditHotel", viewModel);
                    }
                }

                try
                {
                    _context.Update(hotelToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Hotel '{hotelToUpdate.Name}' updated successfully!";
                    _logger.LogInformation($"Hotel ID {hotelToUpdate.Id} ('{hotelToUpdate.Name}') updated by admin.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelExists(hotelToUpdate.Id))
                    {
                        _logger.LogWarning($"EditHotel POST: Concurrency error, Hotel ID {hotelToUpdate.Id} not found.");
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError($"Concurrency error updating hotel ID {hotelToUpdate.Id}.");
                        TempData["ErrorMessage"] = "Error updating hotel. It might have been modified by another user. Please try again.";
                        return View("EditHotel", viewModel);
                    }
                }
                return RedirectToAction(nameof(SearchResults), new { destination = hotelToUpdate.Country });
            }

            _logger.LogWarning($"EditHotel POST: ModelState is invalid for Hotel ID {id}.");
            if (string.IsNullOrEmpty(viewModel.ExistingImagePath) && id > 0)
            {
                var existingHotel = await _context.Hotels.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
                if (existingHotel != null)
                {
                    viewModel.ExistingImagePath = existingHotel.ImagePath;
                }
            }
            return View("EditHotel", viewModel);
        }

        private bool HotelExists(int id) => _context.Hotels.Any(e => e.Id == id);


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

        public async Task<IActionResult> SearchResults(
           string? destination,
           string? selectedDates, 
           string? selectedGuests,  
           int? minRating,          
           decimal? minPrice,      
           decimal? maxPrice,       
           string? sortBy = "name",
           string? sortOrder = "asc",
           int pageNumber = 1)
        {
            _logger.LogInformation($"Search Request: Dest='{destination}', Dates='{selectedDates}', Guests='{selectedGuests}', MinRating='{minRating}', MinPrice='{minPrice}', MaxPrice='{maxPrice}', SortBy='{sortBy}', SortOrder='{sortOrder}', Page='{pageNumber}'");

            string decodedDestination = WebUtility.UrlDecode(destination ?? "");
            string decodedGuests = WebUtility.UrlDecode(selectedGuests ?? "");
            string decodedDates = WebUtility.UrlDecode(selectedDates ?? "Any");

            int pageSize = 6;

            IQueryable<Hotel> query = _context.Hotels.AsQueryable();

            if (!string.IsNullOrEmpty(decodedDestination))
            {
                query = query.Where(h => h.Country.ToLower().Contains(decodedDestination.ToLower()) ||
                                         h.City.ToLower().Contains(decodedDestination.ToLower()));
            }

            if (minRating.HasValue && minRating.Value > 0)
            {
                query = query.Where(h => h.StarRating >= minRating.Value);
            }

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(h => h.PricePerNight >= minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(h => h.PricePerNight <= maxPrice.Value);
            }

            switch (sortBy?.ToLower())
            {
                case "price":
                    query = sortOrder?.ToLower() == "desc" ? query.OrderByDescending(h => h.PricePerNight) : query.OrderBy(h => h.PricePerNight);
                    break;
                case "rating":
                    query = sortOrder?.ToLower() == "desc" ? query.OrderByDescending(h => h.StarRating) : query.OrderBy(h => h.StarRating);
                    break;
                case "score":
                    query = sortOrder?.ToLower() == "desc" ? query.OrderByDescending(h => h.ReviewScore ?? 0.0) : query.OrderBy(h => h.ReviewScore ?? 0.0);
                    break;
                case "name":
                default:
                    sortBy = "name";
                    query = sortOrder?.ToLower() == "desc" ? query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name);
                    break;
            }

            int totalItemCount = await query.CountAsync();

            var hotelsFromDb = await query
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            var hotelResults = new List<HotelResultViewModel>();
            foreach (var hotel in hotelsFromDb)
            {
                hotelResults.Add(new HotelResultViewModel
                {
                    Id = hotel.Id,
                    HotelName = hotel.Name,
                    ImageUrl = hotel.ImagePath,
                    StarRating = hotel.StarRating,
                    LocationName = $"{hotel.City}, {hotel.Country}",
                    ReviewScore = (decimal)(hotel.ReviewScore ?? 0.0),
                    ReviewScoreText = GetReviewText((decimal)(hotel.ReviewScore ?? 0.0)),
                    PricePerNight = hotel.PricePerNight,
                    CurrencySymbol = "BGN",
                });
            }

            var viewModel = new SearchResultsViewModel
            {
                SearchDestination = decodedDestination,
                SearchDates = decodedDates,
                SearchGuests = decodedGuests,
                Results = hotelResults,

                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalItemCount,
                TotalPages = (int)Math.Ceiling(totalItemCount / (double)pageSize),

                CurrentSortField = sortBy,
                CurrentSortOrder = sortOrder,

                MinRating = minRating,
                MinPrice = minPrice,
                MaxPrice = maxPrice
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

    

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        

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

        [Authorize(Roles = "Admin")] 
        [HttpPost, ActionName("DeleteHotel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHotelConfirmed(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                _logger.LogWarning($"DeleteHotel POST: Hotel with ID {id} not found for deletion by admin.");
                TempData["ErrorMessage"] = $"Hotel with ID {id} not found.";
                return RedirectToAction(nameof(SearchResults)); 
            }

            try
            {
                var bookingsExist = await _context.Bookings.AnyAsync(b => b.HotelId == id);
                if (bookingsExist)
                {
                    _logger.LogWarning($"Admin attempt to delete Hotel ID {id} which has existing bookings.");
                    TempData["ErrorMessage"] = "Cannot delete this hotel as it has existing bookings. Please remove associated bookings first.";
                    return RedirectToAction(nameof(DeleteHotel), new { id = id });
                }

                string? imagePathToDelete = hotel.ImagePath;
                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Hotel '{hotel.Name}' was successfully deleted.";
                _logger.LogInformation($"Hotel ID {id}, Name: {hotel.Name} deleted by admin.");

                if (!string.IsNullOrEmpty(imagePathToDelete))
                {
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePathToDelete.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        try { System.IO.File.Delete(fullPath); _logger.LogInformation($"Associated image '{fullPath}' deleted for hotel ID {id}."); }
                        catch (Exception ex) { _logger.LogError(ex, $"Error deleting image file '{fullPath}' for deleted hotel ID {id}."); TempData["WarningMessage"] = "Hotel deleted, but couldn't remove image file."; }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting Hotel ID {id}, Name: {hotel.Name}.");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the hotel.";
                return RedirectToAction(nameof(SearchResults)); 
            }
            return RedirectToAction(nameof(Index)); 
        }
    }
}
