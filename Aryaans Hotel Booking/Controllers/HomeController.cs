using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aryaans_Hotel_Booking.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BookingController> _logger;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<BookingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null || room.Hotel == null)
            {
                TempData["ErrorMessage"] = "Selected room or hotel not found.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new CreateBookingViewModel
            {
                HotelId = room.HotelId,
                HotelName = room.Hotel.Name,
                HotelImageUrl = room.Hotel.ImagePath,
                RoomId = room.Id,
                RoomType = room.RoomType,
                RoomDescription = room.Description,
                RoomPricePerNight = room.PricePerNight,
                RoomCapacity = room.Capacity,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(2),
                NumberOfGuests = 1
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            var room = await _context.Rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == model.RoomId);

            if (room == null || room.Hotel == null)
            {
                ModelState.AddModelError("", "Selected room or hotel could not be found.");
                return View(model);
            }

            model.HotelName = room.Hotel.Name;
            model.HotelImageUrl = room.Hotel.ImagePath;
            model.RoomType = room.RoomType;
            model.RoomDescription = room.Description;
            model.RoomPricePerNight = room.PricePerNight;
            model.RoomCapacity = room.Capacity;

            if (model.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");
            }
            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out date must be after the check-in date.");
            }
            if (model.NumberOfGuests > room.Capacity)
            {
                ModelState.AddModelError("NumberOfGuests", $"Number of guests exceeds the capacity of this room ({room.Capacity}).");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Booking creation failed model validation.");
                return View(model);
            }

            var overlappingBookings = await _context.Bookings
                .AnyAsync(b => b.RoomId == model.RoomId &&
                                b.CheckInDate < model.CheckOutDate &&
                                b.CheckOutDate > model.CheckInDate);

            if (overlappingBookings)
            {
                ModelState.AddModelError("", "Sorry, this room is not available for the selected dates.");
                _logger.LogWarning($"Overlapping booking detected for RoomId: {model.RoomId} between {model.CheckInDate} and {model.CheckOutDate}");
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var numberOfNights = (int)(model.CheckOutDate - model.CheckInDate).TotalDays;
            var totalPrice = numberOfNights * room.PricePerNight;

            var booking = new Booking
            {
                HotelId = room.HotelId,
                RoomId = model.RoomId,
                UserId = currentUser.Id,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                NumberOfGuests = model.NumberOfGuests,
                TotalPrice = totalPrice,
                BookingDate = DateTime.UtcNow
            };

            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New booking created with ID: {booking.Id} by User: {currentUser.UserName}");
                TempData["SuccessMessage"] = $"Your booking for {room.Hotel.Name} ({room.RoomType}) from {model.CheckInDate:MMMM dd} to {model.CheckOutDate:MMMM dd} has been confirmed!";
                return RedirectToAction("MyBookings");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving booking to database.");
                ModelState.AddModelError("", "Could not save your booking due to a database error. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating the booking.");
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var bookings = await _context.Bookings
                .Where(b => b.UserId == currentUser.Id)
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }
    }
}
