using Aryaans_Hotel_Booking.Areas.Admin.Models;
using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Models;
using Aryaans_Hotel_Booking.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Aryaans_Hotel_Booking.Services
{
    public class HotelService : IHotelService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<HotelService> _logger;

        public HotelService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<HotelService> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<IEnumerable<HotelResultViewModel>> GetFeaturedHotelsAsync(int count)
        {
            var featuredHotelsFromDb = await _context.Hotels
                .OrderByDescending(h => h.StarRating)
                .ThenByDescending(h => h.ReviewScore)
                .Take(count)
                .ToListAsync();
            return featuredHotelsFromDb.Select(MapHotelToResultViewModel).ToList();
        }

        public async Task<SearchResultsViewModel> SearchHotelsAsync(
            string? destination, string? selectedDates, string? selectedGuests,
            int? minRating, decimal? minPrice, decimal? maxPrice,
            string? sortBy, string? sortOrder, int pageNumber, int pageSize)
        {
            string decodedDestination = WebUtility.UrlDecode(destination ?? "");
            IQueryable<Hotel> query = _context.Hotels.Include(h => h.Rooms).AsQueryable();

            if (!string.IsNullOrEmpty(decodedDestination))
            {
                query = query.Where(h => (h.Country != null && h.Country.ToLower().Contains(decodedDestination.ToLower())) ||
                                         (h.City != null && h.City.ToLower().Contains(decodedDestination.ToLower())));
            }
            if (minRating.HasValue && minRating.Value > 0) query = query.Where(h => h.StarRating >= minRating.Value);
            if (minPrice.HasValue && minPrice.Value > 0) query = query.Where(h => h.PricePerNight >= minPrice.Value);
            if (maxPrice.HasValue && maxPrice.Value > 0) query = query.Where(h => h.PricePerNight <= maxPrice.Value);

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
            var hotelResults = hotelsFromDb.Select(MapHotelToResultViewModel).ToList();

            return new SearchResultsViewModel
            {
                SearchDestination = decodedDestination,
                SearchDates = WebUtility.UrlDecode(selectedDates ?? "Any"),
                SearchGuests = WebUtility.UrlDecode(selectedGuests ?? ""),
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
        }

        public async Task<HotelResultViewModel?> GetHotelDetailsViewModelAsync(int hotelId)
        {
            var hotel = await _context.Hotels
                                      .Include(h => h.Rooms)
                                      .FirstOrDefaultAsync(h => h.Id == hotelId);
            if (hotel == null) return null;

            var hotelViewModel = MapHotelToResultViewModel(hotel);
            return hotelViewModel;
        }

        public async Task<Aryaans_Hotel_Booking.Models.AddHotelViewModel> PrepareAddHotelViewModelAsync()
        {
            return await Task.FromResult(new Aryaans_Hotel_Booking.Models.AddHotelViewModel());
        }

        public async Task<int> AddHotelAsync(Aryaans_Hotel_Booking.Models.AddHotelViewModel model)
        {
            string? virtualImagePath = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                virtualImagePath = await SaveImageAsync(model.Image);
                if (virtualImagePath == null) return 0;
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
                ReviewScore = (double?)model.ReviewScore,
                ImagePath = virtualImagePath
            };

            try
            {
                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel '{hotel.Name}' (ID: {hotel.Id}) added by service.");
                return hotel.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding hotel '{model.Name}' via service.");
                return 0;
            }
        }

        public async Task<Aryaans_Hotel_Booking.Models.EditHotelViewModel?> GetHotelForEditAsync(int hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return null;
            return new Aryaans_Hotel_Booking.Models.EditHotelViewModel
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
        }

        public async Task<bool> UpdateHotelAsync(Aryaans_Hotel_Booking.Models.EditHotelViewModel model)
        {
            var hotelToUpdate = await _context.Hotels.FindAsync(model.Id);
            if (hotelToUpdate == null) return false;

            hotelToUpdate.Name = model.Name;
            hotelToUpdate.Country = model.Country;
            hotelToUpdate.City = model.City;
            hotelToUpdate.Address = model.Address;
            hotelToUpdate.Description = model.Description;
            hotelToUpdate.StarRating = model.StarRating;
            hotelToUpdate.PricePerNight = model.PricePerNight;
            hotelToUpdate.ReviewScore = (double?)model.ReviewScore;

            if (model.NewImage != null && model.NewImage.Length > 0)
            {
                if (!string.IsNullOrEmpty(hotelToUpdate.ImagePath)) DeleteImage(hotelToUpdate.ImagePath);
                hotelToUpdate.ImagePath = await SaveImageAsync(model.NewImage);
                if (hotelToUpdate.ImagePath == null && model.NewImage != null) return false;
            }

            try
            {
                _context.Update(hotelToUpdate);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel ID {hotelToUpdate.Id} updated by service.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating hotel ID {model.Id} via service.");
                return false;
            }
        }

        public async Task<Hotel?> GetHotelForDeletionAsync(int hotelId)
        {
            return await _context.Hotels.FindAsync(hotelId);
        }

        public async Task<(bool Success, string? OldImagePath)> DeleteHotelAsync(int hotelId)
        {
            var hotel = await _context.Hotels.Include(h => h.Rooms).FirstOrDefaultAsync(h => h.Id == hotelId);
            if (hotel == null) return (false, null);

            var bookingsExistForHotelRooms = await _context.Bookings.AnyAsync(b => hotel.Rooms.Select(r => r.Id).Contains(b.RoomId));
            if (bookingsExistForHotelRooms)
            {
                _logger.LogWarning($"Attempt to delete Hotel ID {hotelId} with bookings.");
                return (false, null);
            }

            string? imagePathToDelete = hotel.ImagePath;
            try
            {
                if (hotel.Rooms.Any()) _context.Rooms.RemoveRange(hotel.Rooms);
                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel ID {hotelId} deleted by service.");
                if (!string.IsNullOrEmpty(imagePathToDelete)) DeleteImage(imagePathToDelete);
                return (true, imagePathToDelete);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"DB error deleting hotel ID {hotelId}.");
                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Generic error deleting hotel ID {hotelId}.");
                return (false, null);
            }
        }

        public async Task<IEnumerable<HotelResultViewModel>> GetAllHotelsForAdminAsync()
        {
            var hotels = await _context.Hotels.OrderBy(h => h.Name).ToListAsync();
            return hotels.Select(MapHotelToResultViewModel).ToList();
        }

        public async Task<int> CreateRoomAsync(Aryaans_Hotel_Booking.Models.AddRoomViewModel model)
        {
            var hotelExists = await _context.Hotels.AnyAsync(h => h.Id == model.HotelId);
            if (!hotelExists) return 0;

            var room = new Room
            {
                HotelId = model.HotelId,
                RoomType = model.RoomType,
                PricePerNight = model.PricePerNight,
                Capacity = model.Capacity,
                Description = model.Description,
                IsAvailable = model.IsAvailable,
            };

            try
            {
                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
                return room.Id;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error creating room."); return 0; }
        }

        public async Task<Aryaans_Hotel_Booking.Models.EditRoomViewModel?> GetRoomByIdForEditAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return null;
            return new Aryaans_Hotel_Booking.Models.EditRoomViewModel
            {
                Id = room.Id,
                HotelId = room.HotelId,
                RoomType = room.RoomType,
                PricePerNight = room.PricePerNight,
                Capacity = room.Capacity,
                Description = room.Description,
                IsAvailable = room.IsAvailable
            };
        }

        public async Task<bool> UpdateRoomAsync(Aryaans_Hotel_Booking.Models.EditRoomViewModel model)
        {
            var roomToUpdate = await _context.Rooms.FindAsync(model.Id);
            if (roomToUpdate == null) return false;

            roomToUpdate.RoomType = model.RoomType;
            roomToUpdate.PricePerNight = model.PricePerNight;
            roomToUpdate.Capacity = model.Capacity;
            roomToUpdate.Description = model.Description;
            roomToUpdate.IsAvailable = model.IsAvailable;

            try
            {
                _context.Update(roomToUpdate);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error updating room."); return false; }
        }

        public async Task<bool> DeleteRoomAsync(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;
            var bookingsExist = await _context.Bookings.AnyAsync(b => b.RoomId == roomId);
            if (bookingsExist) { _logger.LogWarning($"Cannot delete room {roomId}, bookings exist."); return false; }

            try
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error deleting room."); return false; }
        }

        public async Task<IEnumerable<RoomViewModel>> GetRoomsForHotelAsync(int hotelId)
        {
            return await _context.Rooms
                .Where(r => r.HotelId == hotelId)
                .Select(r => new RoomViewModel
                {
                    Id = r.Id,
                    RoomType = r.RoomType,
                    PricePerNight = r.PricePerNight,
                    Capacity = r.Capacity,
                    IsAvailable = r.IsAvailable,
                    Amenities = r.Description
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Hotel>> GetAllHotelsForApiAsync() => await _context.Hotels.Include(h => h.Rooms).ToListAsync();
        public async Task<Hotel?> GetHotelByIdForApiAsync(int hotelId) => await _context.Hotels.Include(h => h.Rooms).FirstOrDefaultAsync(h => h.Id == hotelId);

        public async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;
            string serverUploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels");
            Directory.CreateDirectory(serverUploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string serverFilePath = Path.Combine(serverUploadsFolder, uniqueFileName);
            try
            {
                using (var fileStream = new FileStream(serverFilePath, FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
                return $"/images/hotels/{uniqueFileName}";
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error saving image '{imageFile.FileName}'."); return null; }
        }
        public void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
            if (File.Exists(fullPath)) { try { File.Delete(fullPath); } catch (Exception ex) { _logger.LogError(ex, $"Error deleting image '{fullPath}'."); } }
        }

        private HotelResultViewModel MapHotelToResultViewModel(Hotel hotel)
        {
            return new HotelResultViewModel
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
                ReviewCount = _context.Bookings.Count(b => b.HotelId == hotel.Id)
            };
        }
        private string GetReviewText(decimal score)
        {
            if (score >= 9.0m) return "Superb";
            if (score >= 8.0m) return "Very Good";
            if (score >= 7.0m) return "Good";
            if (score >= 6.0m) return "Pleasant";
            return score <= 0.0m ? "" : "Okay";
        }
    }
}