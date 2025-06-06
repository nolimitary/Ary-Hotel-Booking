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

            builder.Entity<Hotel>()
                .HasData(new Hotel
                {
                    Id = 1,
                    Name = "Aryaans Hotel",
                    Country = "India",
                    City = "Delhi",
                    Address = "123 Aryaans Street, Delhi",
                    Description = "A luxurious hotel in the heart of Delhi.",
                    PricePerNight = 5000.00m,
                    StarRating = 5,
                    ReviewScore = 9.5,
                    ImagePath = "images/hotel1.jpg"


                },
                new Hotel()
                {
                    Id = 2,
                    Name = "Aryaans Resort",
                    Country = "India",
                    City = "Goa",
                    Address = "456 Aryaans Avenue, Goa",
                    Description = "A beautiful resort by the beach.",
                    PricePerNight = 7000.00m,
                    StarRating = 4,
                    ReviewScore = 8.8,
                    ImagePath = "images/hotel2.jpg"

                },
                new Hotel()
                {
                    Id = 3,
                    Name = "Aryaans Palace",
                    Country = "India",
                    City = "Mumbai",
                    Address = "789 Aryaans Boulevard, Mumbai",
                    Description = "A royal experience in the city of dreams.",
                    PricePerNight = 10000.00m,
                    StarRating = 5,
                    ReviewScore = 9.0,
                    ImagePath = "images/hotel3.jpg"
                },
                new Hotel()
                {
                    Id = 4,
                    Name = "Aryaans Heritage Hotel",
                    Country = "India",
                    City = "Jaipur",
                    Address = "101 Aryaans Heritage Road, Jaipur",
                    Description = "A heritage hotel with royal architecture.",
                    PricePerNight = 6000.00m,
                    StarRating = 4,
                    ReviewScore = 8.5,
                    ImagePath = "images/hotel4.jpg"
                },
                new Hotel()
                {
                    Id = 5,
                    Name = "Aryaans Mountain Resort",
                    Country = "India",
                    City = "Shimla",
                    Address = "202 Aryaans Hilltop, Shimla",
                    Description = "A serene resort in the mountains.",
                    PricePerNight = 8000.00m,
                    StarRating = 4,
                    ReviewScore = 9.2,
                    ImagePath = "images/hotel5.jpg"
                },
                new Hotel()
                {
                    Id = 6,
                    Name = "Aryaans Urban Hotel",
                    Country = "India",
                    City = "Bangalore",
                    Address = "303 Aryaans Tech Park, Bangalore",
                    Description = "A modern hotel in the tech city.",
                    PricePerNight = 5500.00m,
                    StarRating = 4,
                    ReviewScore = 8.7,
                    ImagePath = "images/hotel6.jpg"
                },
                new Hotel()
                {
                    Id = 7,
                    Name = "Aryaans Coastal Retreat",
                    Country = "India",
                    City = "Chennai",
                    Address = "404 Aryaans Beach Road, Chennai",
                    Description = "A coastal retreat with stunning sea views.",
                    PricePerNight = 6500.00m,
                    StarRating = 4,
                    ReviewScore = 8.9,
                    ImagePath = "images/hotel7.jpg"
                },
                new Hotel()
                {
                    Id = 8,
                    Name = "Aryaans Desert Oasis",
                    Country = "India",
                    City = "Jaisalmer",
                    Address = "505 Aryaans Desert Road, Jaisalmer",
                    Description = "A unique desert experience with luxury amenities.",
                    PricePerNight = 7500.00m,
                    StarRating = 5,
                    ReviewScore = 9.3,
                    ImagePath = "images/hotel8.jpg"
                }

                );
        }
    }
}