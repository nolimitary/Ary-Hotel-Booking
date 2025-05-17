using Microsoft.EntityFrameworkCore;
using Aryaans_Hotel_Booking.Data.Entities;

namespace Aryaans_Hotel_Booking.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; } 
    }
}