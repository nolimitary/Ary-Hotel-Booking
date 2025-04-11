namespace Aryaans_Hotel_Booking.Models 
{
    public class DatePickerViewModel
    {
        public DateTime StartDate { get; set; } 
        public int NumberOfMonths { get; set; }
        public List<MonthViewModel> Months { get; set; } = new List<MonthViewModel>();
    }
}