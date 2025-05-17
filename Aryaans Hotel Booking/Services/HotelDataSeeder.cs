using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Aryaans_Hotel_Booking.Services
{
    public class HotelDataSeeder
    {

        public static async Task SeedHotelsAsync(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment )
        {
            if (await context.Hotels.AnyAsync())
            {
                Console.WriteLine("Database already seeded with hotels. Skipping.");
                return;
            }

            var hotelsFilePath = Path.Combine(webHostEnvironment.WebRootPath, "hotels.txt");
            if (!File.Exists(hotelsFilePath))
            {
                Console.WriteLine($"hotels.txt not found at {hotelsFilePath}. Skipping seeding.");
                return;
            }

            var hotelsToSeed = new List<Hotel>();
            var lines = await File.ReadAllLinesAsync(hotelsFilePath);

            Console.WriteLine($"Reading hotels from {hotelsFilePath}.");

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var hotelData = line.Split(',');
                if (hotelData.Length >= 7)
                {
                    try
                    {
                        var hotel = new Hotel
                        {
                            Name = hotelData[0].Trim(),
                            Country = hotelData[1].Trim(),
                            City = hotelData[2].Trim(),
                            PricePerNight = decimal.Parse(hotelData[3].Trim(), CultureInfo.InvariantCulture),
                            StarRating = int.Parse(hotelData[4].Trim()),
                            ReviewScore = double.Parse(hotelData[5].Trim(), CultureInfo.InvariantCulture),
                            ImagePath = hotelData[6].Trim()
                        };

                        if (!hotelsToSeed.Any(h => h.Name == hotel.Name && h.City == hotel.City && h.Country == hotel.Country))
                        {
                            hotelsToSeed.Add(hotel);
                        }
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Error parsing hotel data line: '{line}'. Error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred for line: '{line}'. Error: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping malformed hotel data line: '{line}'. Not enough data parts.");
                }
            }

            if (hotelsToSeed.Any())
            {
                await context.Hotels.AddRangeAsync(hotelsToSeed);
                await context.SaveChangesAsync();
                Console.WriteLine($"Seeded {hotelsToSeed.Count} hotels from hotels.txt.");
            }
            else
            {
                Console.WriteLine("No new hotels to seed from hotels.txt or file was empty/malformed.");
            }
        }
    }
}