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

            var featuredHotelViewModels = new List<HotelResultViewModel>();
            foreach (var hotel in featuredHotelsFromDb)
            {
                featuredHotelViewModels.Add(MapHotelToResultViewModel(hotel));
            }
            return featuredHotelViewModels;
        }

        public async Task<SearchResultsViewModel> SearchHotelsAsync(
            string? destination, string? selectedDates, string? selectedGuests,
            int? minRating, decimal? minPrice, decimal? maxPrice,
            string? sortBy, string? sortOrder, int pageNumber, int pageSize)
        {
            string decodedDestination = WebUtility.UrlDecode(destination ?? "");

            IQueryable<Hotel> query = _context.Hotels.AsQueryable();

            if (!string.IsNullOrEmpty(decodedDestination))
            {
                query = query.Where(h => (h.Country != null && h.Country.ToLower().Contains(decodedDestination.ToLower())) ||
                                         (h.City != null && h.City.ToLower().Contains(decodedDestination.ToLower())));
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

            var hotelResults = hotelsFromDb.Select(hotel => MapHotelToResultViewModel(hotel)).ToList();

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

        public async Task<AddHotelViewModel> PrepareAddHotelViewModelAsync()
        {
            return await Task.FromResult(new AddHotelViewModel());
        }

        public async Task<bool> AddHotelAsync(AddHotelViewModel model)
        {
            string? virtualImagePath = null;
            if (model.Image != null && model.Image.Length > 0)
            {
                virtualImagePath = await SaveImageAsync(model.Image);
                if (virtualImagePath == null)
                {
                    return false;
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
                ReviewScore = (double?)model.ReviewScore,
                ImagePath = virtualImagePath
            };

            try
            {
                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel '{hotel.Name}' (ID: {hotel.Id}) added successfully by service.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding hotel '{model.Name}' via service.");
                return false;
            }
        }

        public async Task<EditHotelViewModel?> GetHotelForEditAsync(int hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return null;

            return new EditHotelViewModel
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

        public async Task<bool> UpdateHotelAsync(EditHotelViewModel model)
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
                if (!string.IsNullOrEmpty(hotelToUpdate.ImagePath))
                {
                    DeleteImage(hotelToUpdate.ImagePath);
                }
                hotelToUpdate.ImagePath = await SaveImageAsync(model.NewImage);
                if (hotelToUpdate.ImagePath == null && model.NewImage != null)
                {
                    return false;
                }
            }

            try
            {
                _context.Update(hotelToUpdate);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel ID {hotelToUpdate.Id} ('{hotelToUpdate.Name}') updated by service.");
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
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return (false, null);

            var bookingsExist = await _context.Bookings.AnyAsync(b => b.HotelId == hotelId);
            if (bookingsExist)
            {
                _logger.LogWarning($"Attempt to delete Hotel ID {hotelId} which has existing bookings, via service.");
                return (false, null);
            }

            string? imagePathToDelete = hotel.ImagePath;
            try
            {
                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Hotel ID {hotelId} ('{hotel.Name}') deleted by service.");
                if (!string.IsNullOrEmpty(imagePathToDelete))
                {
                    DeleteImage(imagePathToDelete);
                }
                return (true, imagePathToDelete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting hotel ID {hotelId} via service.");
                return (false, null);
            }
        }

        public async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            string serverUploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "hotels");
            Directory.CreateDirectory(serverUploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string serverFilePath = Path.Combine(serverUploadsFolder, uniqueFileName);

            try
            {
                using (var fileStream = new FileStream(serverFilePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                return $"/images/hotels/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving image '{imageFile.FileName}' in service.");
                return null;
            }
        }

        public void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                    _logger.LogInformation($"Image '{fullPath}' deleted by service.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting image file '{fullPath}' in service.");
                }
            }
        }

        public async Task<IEnumerable<Hotel>> GetAllHotelsForApiAsync()
        {
            return await _context.Hotels.ToListAsync();
        }

        public async Task<Hotel?> GetHotelByIdForApiAsync(int hotelId)
        {
            return await _context.Hotels.FindAsync(hotelId);
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
                CurrencySymbol = "BGN"
            };
        }

        private string GetReviewText(decimal score)
        {
            if (score >= 9.0m) return "Superb";
            if (score >= 8.0m) return "Very Good";
            if (score >= 7.0m) return "Good";
            if (score >= 6.0m) return "Pleasant";
            if (score <= 0.0m) return "";
            return "Okay";
        }
    }
}