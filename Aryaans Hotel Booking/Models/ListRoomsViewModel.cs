using System.Collections.Generic;

namespace Aryaans_Hotel_Booking.Models
{
    public class ListRoomsViewModel
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; }
        public List<RoomViewModel> Rooms { get; set; }

        public ListRoomsViewModel()
        {
            Rooms = new List<RoomViewModel>();
        }
    }
}