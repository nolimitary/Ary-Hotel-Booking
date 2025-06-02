using Aryaans_Hotel_Booking.Models;
using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aryaans_Hotel_Booking.Services.Interfaces
{
    public interface IHotelService
    {
        Task<IEnumerable<HotelResultViewModel>> GetFeaturedHotelsAsync(int count);
        Task<SearchResultsViewModel> SearchHotelsAsync(
            string? destination,
            string? selectedDates,
            string? selectedGuests,
            int? minRating,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            string? sortOrder,
            int pageNumber,
            int pageSize);

        Task<AddHotelViewModel> PrepareAddHotelViewModelAsync();
        Task<bool> AddHotelAsync(AddHotelViewModel model);
        Task<EditHotelViewModel?> GetHotelForEditAsync(int hotelId);
        Task<bool> UpdateHotelAsync(EditHotelViewModel model);
        Task<Hotel?> GetHotelForDeletionAsync(int hotelId);
        Task<(bool Success, string? OldImagePath)> DeleteHotelAsync(int hotelId);

        Task<IEnumerable<Hotel>> GetAllHotelsForApiAsync();
        Task<Hotel?> GetHotelByIdForApiAsync(int hotelId);

        Task<string?> SaveImageAsync(IFormFile imageFile);
        void DeleteImage(string? imagePath);
    }
}