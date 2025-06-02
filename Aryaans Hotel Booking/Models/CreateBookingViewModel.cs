using System;
using System.ComponentModel.DataAnnotations;

namespace Aryaans_Hotel_Booking.Models
{
    public class CreateBookingViewModel
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; }
        public string HotelImageUrl { get; set; }

        public int RoomId { get; set; }
        public string RoomType { get; set; }
        public string RoomDescription { get; set; }
        public decimal RoomPricePerNight { get; set; }
        public int RoomCapacity { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in Date")]
        public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out Date")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

        [Required]
        [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10.")]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; }

        public decimal TotalPrice { get; set; }
        public int NumberOfNights { get; set; }
    }
}