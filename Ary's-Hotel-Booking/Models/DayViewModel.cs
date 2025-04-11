namespace Ary_s_Hotel_Booking.Models 
{
    public class DayViewModel
    {
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public bool IsPlaceholder { get; set; } = false;
        public bool IsToday { get; set; } = false;
        public bool IsDisabled { get; set; } = false; 
    }
}