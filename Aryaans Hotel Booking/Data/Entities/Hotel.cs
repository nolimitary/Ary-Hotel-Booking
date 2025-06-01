using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class Hotel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Country { get; set; }

        [Required]
        [MaxLength(50)]
        public string City { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required] 
        [Column(TypeName = "decimal(18, 2)")] 
        public decimal PricePerNight { get; set; } 

        [Required]
        [Range(1, 5)]
        public int StarRating { get; set; }

        [Range(0, 10)]
        public double? ReviewScore { get; set; }

        [MaxLength(255)]
        public string ImagePath { get; set; }

        public virtual ICollection<Room> Rooms { get; set; } = new HashSet<Room>();
        public virtual ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();
    }
}
