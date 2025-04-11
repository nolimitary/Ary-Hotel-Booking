using Ary_s_Hotel_Booking.Models;
using Ary_s_Hotel_Booking.Models ;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace Aryaans_Hotel_Booking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index(string? selectedDates, string? selectedGuests, string? selectedLocation)
        {
            ViewData["SelectedDates"] = selectedDates;

            string? displayGuests = !string.IsNullOrEmpty(selectedGuests)
                                      ? WebUtility.UrlDecode(selectedGuests)
                                      : null;
            ViewData["SelectedGuests"] = displayGuests;

            string? displayLocation = !string.IsNullOrEmpty(selectedLocation)
                                     ? WebUtility.UrlDecode(selectedLocation)
                                     : null;
            ViewData["SelectedLocation"] = displayLocation;

            return View();
        }

        public IActionResult DatePicker()
        {
            var startDate = DateTime.Today;
            int numberOfMonths = 12;
            var viewModel = new DatePickerViewModel { StartDate = startDate, NumberOfMonths = numberOfMonths };
            DateTime currentMonthDate = new DateTime(startDate.Year, startDate.Month, 1);

            for (int i = 0; i < numberOfMonths; i++)
            {
                var monthViewModel = new MonthViewModel
                {
                    Year = currentMonthDate.Year,
                    Month = currentMonthDate.Month,
                    MonthName = currentMonthDate.ToString("MMMM", CultureInfo.InvariantCulture)
                };

                DayOfWeek firstDayOfMonth = currentMonthDate.DayOfWeek;
                int placeholders = ((int)firstDayOfMonth - (int)DayOfWeek.Monday + 7) % 7;

                for (int p = 0; p < placeholders; p++)
                {
                    monthViewModel.Days.Add(new DayViewModel { IsPlaceholder = true });
                }

                int daysInMonth = DateTime.DaysInMonth(currentMonthDate.Year, currentMonthDate.Month);

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var dayDate = new DateTime(currentMonthDate.Year, currentMonthDate.Month, day);
                    monthViewModel.Days.Add(new DayViewModel
                    {
                        DayNumber = day,
                        Date = dayDate,
                        IsToday = (dayDate == DateTime.Today),
                        IsDisabled = (dayDate < DateTime.Today)
                    });
                }

                viewModel.Months.Add(monthViewModel);
                currentMonthDate = currentMonthDate.AddMonths(1);
            }

            return View(viewModel);
        }


        public IActionResult GuestPicker()
        {
            return View();
        }

        public IActionResult DestinationPicker()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}