using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aryaans_Hotel_Booking.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string> 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }

        public DbSet<Booking> Bookings { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasMany(u => u.Bookings)
                      .WithOne(b => b.User)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Hotel>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
                entity.Property(h => h.Country).IsRequired().HasMaxLength(50);
                entity.Property(h => h.City).IsRequired().HasMaxLength(50);
                entity.Property(h => h.Address).HasMaxLength(200);
                entity.Property(h => h.Description).HasMaxLength(1000);
                entity.Property(h => h.PricePerNight).IsRequired().HasColumnType("decimal(18, 2)");
                entity.Property(h => h.StarRating).IsRequired();
                entity.Property(h => h.ReviewScore);
                entity.Property(h => h.ImagePath).HasMaxLength(255);

                entity.HasMany(h => h.Bookings)
                      .WithOne(b => b.Hotel)
                      .HasForeignKey(b => b.HotelId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(h => h.Rooms)
                      .WithOne(r => r.Hotel)
                      .HasForeignKey(r => r.HotelId)
                      .OnDelete(DeleteBehavior.Cascade);

                
            });

            builder.Entity<Room>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.RoomNumber);
                entity.Property(r => r.RoomType).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Description).HasMaxLength(200);
                entity.Property(r => r.PricePerNight).IsRequired().HasColumnType("decimal(18, 2)");
                entity.Property(r => r.Capacity).IsRequired();
                entity.Property(r => r.IsAvailable);
                entity.Property(r => r.Amenities);


            });

            builder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.CheckInDate).IsRequired();
                entity.Property(b => b.CheckOutDate).IsRequired();
                entity.Property(b => b.NumberOfGuests).IsRequired();
                entity.Property(b => b.TotalPrice).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(b => b.BookingDate).IsRequired();

                entity.Property(b => b.UserId).IsRequired();
                entity.Property(b => b.RoomId).IsRequired();

                entity.HasOne(b => b.Room)
                      .WithMany()
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}