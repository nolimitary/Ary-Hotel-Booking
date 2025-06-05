using Aryaans_Hotel_Booking.Areas.Admin.Models;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Models;

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
        Task<HotelResultViewModel?> GetHotelDetailsViewModelAsync(int hotelId);

        Task<Aryaans_Hotel_Booking.Models.AddHotelViewModel> PrepareAddHotelViewModelAsync();
        Task<int> AddHotelAsync(Aryaans_Hotel_Booking.Models.AddHotelViewModel model);
        Task<Aryaans_Hotel_Booking.Models.EditHotelViewModel?> GetHotelForEditAsync(int hotelId);
        Task<bool> UpdateHotelAsync(Aryaans_Hotel_Booking.Models.EditHotelViewModel model);
        Task<Hotel?> GetHotelForDeletionAsync(int hotelId);
        Task<(bool Success, string? OldImagePath)> DeleteHotelAsync(int hotelId);
        Task<IEnumerable<HotelResultViewModel>> GetAllHotelsForAdminAsync();

        Task<int> CreateRoomAsync(Aryaans_Hotel_Booking.Models.AddRoomViewModel model);
        Task<Aryaans_Hotel_Booking.Models.EditRoomViewModel?> GetRoomByIdForEditAsync(int roomId);
        Task<bool> UpdateRoomAsync(Aryaans_Hotel_Booking.Models.EditRoomViewModel model);
        Task<bool> DeleteRoomAsync(int roomId);
        Task<IEnumerable<RoomViewModel>> GetRoomsForHotelAsync(int hotelId);

        Task<IEnumerable<Hotel>> GetAllHotelsForApiAsync();
        Task<Hotel?> GetHotelByIdForApiAsync(int hotelId);

        Task<string?> SaveImageAsync(IFormFile imageFile);
        void DeleteImage(string? imagePath);
    }
}
