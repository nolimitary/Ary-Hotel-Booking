using System.Collections.Generic;
using Aryaans_Hotel_Booking.Models; 

namespace Aryaans_Hotel_Booking.Models
{
    public class SearchResultsViewModel
    {
        public string? SearchDestination { get; set; }
        public string? SearchDates { get; set; }
        public string? SearchGuests { get; set; }

        public List<HotelResultViewModel> Results { get; set; } = new List<HotelResultViewModel>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; } 

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string? CurrentSortField { get; set; }
        public string? CurrentSortOrder { get; set; }


        public string GetOppositeSortOrder(string field)
        {
            if (string.Equals(CurrentSortField, field, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(CurrentSortOrder, "asc", System.StringComparison.OrdinalIgnoreCase))
            {
                return "desc";
            }
            return "asc"; 
        }
    }
}