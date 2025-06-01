using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class Booking
    {
        public int Id { get; set; }

        public int HotelId { get; set; }
        public virtual Hotel Hotel { get; set; }

        public string UserId { get; set; } 
        public virtual ApplicationUser User { get; set; } 

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    }
}