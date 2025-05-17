using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoomType { get; set; } 

        [StringLength(200)]
        public string? Description { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerNight { get; set; }

        [Required]
        [Range(1, 10)] 
        public int Capacity { get; set; }

        public int HotelId { get; set; }

        [ForeignKey("HotelId")]
        public virtual Hotel Hotel { get; set; }

    }
}