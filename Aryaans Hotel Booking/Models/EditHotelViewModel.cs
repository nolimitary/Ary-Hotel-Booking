using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Aryaans_Hotel_Booking.Models
{
    public class EditHotelViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; }

        [StringLength(200, ErrorMessage = "Address can be up to 200 characters long.")]
        public string? Address { get; set; }

        [StringLength(1000, ErrorMessage = "Description can be up to 1000 characters long.")]
        public string? Description { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Star rating must be between 1 and 5.")]
        public int StarRating { get; set; }

        [Required]
        [Range(0.01, 10000.00, ErrorMessage = "Price per night must be a positive value.")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal PricePerNight { get; set; }

        [Range(0.0, 10.0, ErrorMessage = "Review score must be between 0.0 and 10.0.")]
        [DisplayFormat(DataFormatString = "{0:N1}", ApplyFormatInEditMode = true)]
        public decimal? ReviewScore { get; set; }

        public string? ExistingImagePath { get; set; }

        [Display(Name = "New Hotel Image")]
        public IFormFile? NewImage { get; set; }
    }
}