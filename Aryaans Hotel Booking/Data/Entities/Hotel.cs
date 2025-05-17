using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class Hotel
    {
        [Key] 
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

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerNight { get; set; }

        [Range(1, 5)] 
        public int StarRating { get; set; }

        public double? ReviewScore { get; set; }

        [StringLength(255)] 
        public string ImagePath { get; set; }

    }
}