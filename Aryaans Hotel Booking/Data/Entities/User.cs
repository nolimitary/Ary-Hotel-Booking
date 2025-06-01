using System.Collections.Generic; // Required for ICollection
using System.ComponentModel.DataAnnotations;

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress] 
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();
    }
}