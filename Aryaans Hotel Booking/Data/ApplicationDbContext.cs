using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Hotel>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(h => h.Country)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(h => h.City)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(h => h.PricePerNight) // Hotel's own base PricePerNight
                    .IsRequired()
                    .HasColumnType("decimal(18, 2)");

                entity.Property(h => h.StarRating)
                    .IsRequired();

                entity.Property(h => h.ReviewScore); // Nullable double

                entity.Property(h => h.ImagePath)
                    .HasMaxLength(255);

                entity.HasData(
                    new Hotel
                    {
                        Id = 1,
                        Name = "Grand Hyatt",
                        City = "New York",
                        Country = "USA",
                        PricePerNight = 300.00m, // Example base price for hotel
                        StarRating = 5,
                        ReviewScore = 8.8,
                        ImagePath = "/images/hotels/grand_hyatt.jpg"
                    },
                    new Hotel
                    {
                        Id = 2,
                        Name = "The Plaza",
                        City = "New York",
                        Country = "USA",
                        PricePerNight = 500.00m, // Example base price for hotel
                        StarRating = 5,
                        ReviewScore = 9.2,
                        ImagePath = "/images/hotels/the_plaza.jpg"
                    }
                );
            });

            builder.Entity<Room>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.RoomType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.Description)
                    .HasMaxLength(200);

                entity.Property(r => r.PricePerNight) // Room's specific price
                    .IsRequired()
                    .HasColumnType("decimal(18, 2)");

                entity.Property(r => r.Capacity)
                    .IsRequired(); // Range(1,10) implies required

                entity.HasOne(r => r.Hotel)
                      .WithMany(h => h.Rooms)
                      .HasForeignKey(r => r.HotelId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasData(
                    new Room
                    {
                        Id = 1,
                        HotelId = 1,
                        RoomType = "Deluxe King",
                        Description = "Spacious room with a king-sized bed and city view.",
                        PricePerNight = 450.00m,
                        Capacity = 2
                    },
                    new Room
                    {
                        Id = 2,
                        HotelId = 1,
                        RoomType = "Standard Queen",
                        Description = "Comfortable room with a queen-sized bed.",
                        PricePerNight = 350.00m,
                        Capacity = 2
                    },
                    new Room
                    {
                        Id = 3,
                        HotelId = 2,
                        RoomType = "Plaza Suite",
                        Description = "Luxurious suite with a separate living area and premium amenities.",
                        PricePerNight = 799.99m,
                        Capacity = 4
                    }
                );
            });

            builder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();
            });
        }
    }
}
