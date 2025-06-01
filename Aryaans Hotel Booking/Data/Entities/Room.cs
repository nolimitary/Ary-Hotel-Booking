using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HotelId { get; set; }

        [ForeignKey("HotelId")]
        public virtual Hotel Hotel { get; set; }

        [MaxLength(20)]
        public string RoomNumber { get; set; } 

        [Required]
        [MaxLength(50)]
        public string RoomType { get; set; } 

        [MaxLength(200)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerNight { get; set; }

        [Required]
        [Range(1, 10)]
        public int Capacity { get; set; }

        public bool IsAvailable { get; set; } 

        [MaxLength(500)] 
        public string Amenities { get; set; } 
    }
}
