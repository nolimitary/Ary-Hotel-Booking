using System.ComponentModel.DataAnnotations;

namespace Aryaans_Hotel_Booking.Models
{
    public class AddRoomViewModel
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; }

        [Required(ErrorMessage = "Room number is required.")]
        [StringLength(10, ErrorMessage = "Room number cannot exceed 10 characters.")]
        [Display(Name = "Room Number / Name")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Room type is required.")]
        [StringLength(50, ErrorMessage = "Room type cannot exceed 50 characters.")]
        [Display(Name = "Room Type")]
        public string RoomType { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price per night is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be a positive value.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price Per Night")]
        public decimal PricePerNight { get; set; }

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 10, ErrorMessage = "Capacity must be between 1 and 10.")]
        public int Capacity { get; set; }

        [Display(Name = "Is Currently Available?")]
        public bool IsAvailable { get; set; } = true;

        [StringLength(200, ErrorMessage = "Amenities list cannot exceed 200 characters.")]
        [Display(Name = "Amenities (comma-separated)")]
        public string Amenities { get; set; }
    }
}