using System.Collections.Generic;

namespace Aryaans_Hotel_Booking.Models
{
    public class SearchResultsViewModel
    {
        public string SearchDestination { get; set; }
        public string SearchDates { get; set; }
        public string SearchGuests { get; set; }
        public List<HotelResultViewModel> Results { get; set; } = new List<HotelResultViewModel>();
    }
}