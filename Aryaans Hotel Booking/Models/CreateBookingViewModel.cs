using System;
using System.ComponentModel.DataAnnotations;

namespace Aryaans_Hotel_Booking.Models
{
    public class CreateBookingViewModel
    {
        [Required]
        public int HotelId { get; set; }
        public string HotelName { get; set; }
        public string HotelImageUrl { get; set; }
        public int RoomId { get; set; }
        public string RoomType { get; set; }
        public string RoomDescription { get; set; }
        public decimal RoomPricePerNight { get; set; }
        public int RoomCapacity { get; set; }

        [Required(ErrorMessage = "Check-in date is required.")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Check-out date is required.")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Number of guests is required.")]
        [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10.")]
        public int NumberOfGuests { get; set; }

        [Required(ErrorMessage = "Please enter your full name.")]
        [Display(Name = "Full Name")]
        public string GuestFullName { get; set; }

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string GuestEmail { get; set; }

        public decimal TotalPrice { get; set; }
        public int NumberOfNights { get; set; }
    }
}