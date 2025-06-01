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

                entity.HasData(
                    new Hotel { Id = 1, Name = "Grand Hyatt", Address = "123 Luxury Lane", City = "New York", Country = "USA", Description = "A luxurious hotel in the heart of the city.", PricePerNight = 300.00m, StarRating = 5, ReviewScore = 8.8, ImagePath = "/images/hotels/grand_hyatt.jpg" },
                    new Hotel { Id = 2, Name = "The Plaza", Address = "768 Fifth Avenue", City = "New York", Country = "USA", Description = "Iconic luxury hotel with a rich history.", PricePerNight = 500.00m, StarRating = 5, ReviewScore = 9.2, ImagePath = "/images/hotels/the_plaza.jpg" }
                );
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

                entity.HasOne(r => r.Hotel)
                      .WithMany(h => h.Rooms)
                      .HasForeignKey(r => r.HotelId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasData(
                    new Room { Id = 1, HotelId = 1, RoomNumber = "101", RoomType = "Deluxe King", Description = "Spacious room with a king-sized bed and city view.", PricePerNight = 450.00m, Capacity = 2, IsAvailable = true, Amenities = "WiFi, TV, AC, Minibar" },
                    new Room { Id = 2, HotelId = 1, RoomNumber = "102", RoomType = "Standard Queen", Description = "Comfortable room with a queen-sized bed.", PricePerNight = 350.00m, Capacity = 2, IsAvailable = false, Amenities = "WiFi, TV, AC" },
                    new Room { Id = 3, HotelId = 2, RoomNumber = "201", RoomType = "Plaza Suite", Description = "Luxurious suite with a separate living area and premium amenities.", PricePerNight = 799.99m, Capacity = 4, IsAvailable = true, Amenities = "WiFi, TV, AC, Minibar, Jacuzzi" }
                );
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
            });
        }
    }
}