using System;

namespace Aryaans_Hotel_Booking.Models
{
    public class BookingSuccessViewModel
    {
        public string HotelName { get; set; }
        public string GuestFullName { get; set; }
        public string RoomType { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string BookingReference { get; set; }
    }
}