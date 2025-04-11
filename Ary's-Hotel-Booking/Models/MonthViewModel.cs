namespace Ary_s_Hotel_Booking.Models
{
    public class MonthViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; } 
        public string MonthName { get; set; }
        public List<DayViewModel> Days { get; set; } = new List<DayViewModel>();
    }
}
