using Microsoft.AspNetCore.Identity;
using System.Collections.Generic; 

namespace Aryaans_Hotel_Booking.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {

        public virtual ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();

    }
}