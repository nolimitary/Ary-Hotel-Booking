using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Aryaans_Hotel_Booking.Models
{
    public class AddHotelViewModel
    {
        [Required(ErrorMessage = "Hotel name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(50, ErrorMessage = "Country name cannot be longer than 50 characters.")]
        public string Country { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(50, ErrorMessage = "City name cannot be longer than 50 characters.")]
        public string City { get; set; }

        [StringLength(200, ErrorMessage = "Address can be up to 200 characters long.")]
        public string? Address { get; set; }

        [StringLength(1000, ErrorMessage = "Description can be up to 1000 characters long.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Star rating is required.")]
        [Range(1, 5, ErrorMessage = "Star rating must be between 1 and 5.")]
        public int StarRating { get; set; }

        [Required(ErrorMessage = "Price per night is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be a positive value and less than 10,000.")]
        [DataType(DataType.Currency)]
        public decimal PricePerNight { get; set; }

        [Range(0.0, 10.0, ErrorMessage = "Review score must be between 0.0 and 10.0.")]
        [RegularExpression(@"^\d+(\.\d{1})?$", ErrorMessage = "Review score must be a number with up to one decimal place (e.g., 8.5 or 9).")]
        public decimal? ReviewScore { get; set; }

        [Display(Name = "Hotel Image")]
        public IFormFile? Image { get; set; }
    }
}