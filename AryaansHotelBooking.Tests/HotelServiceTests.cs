using NUnit.Framework;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aryaans_Hotel_Booking.Data;
using Aryaans_Hotel_Booking.Data.Entities;
using Aryaans_Hotel_Booking.Services;
using Aryaans_Hotel_Booking.Services.Interfaces;
using Aryaans_Hotel_Booking.Models;
using System.Net;

[TestFixture]
public class HotelServiceTests
{
    private DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private ApplicationDbContext _dbContext;
    private Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private Mock<ILogger<HotelService>> _mockLogger;
    private IHotelService _hotelService;

    [SetUp]
    public void Setup()
    {
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(_dbContextOptions);
        SeedDatabaseForTesting();

        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockWebHostEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

        _mockLogger = new Mock<ILogger<HotelService>>();

        _hotelService = new HotelService(_dbContext, _mockWebHostEnvironment.Object, _mockLogger.Object);
    }

    private void SeedDatabaseForTesting()
    {
        _dbContext.Hotels.RemoveRange(_dbContext.Hotels);
        _dbContext.Rooms.RemoveRange(_dbContext.Rooms);
        _dbContext.Bookings.RemoveRange(_dbContext.Bookings);
        _dbContext.SaveChanges();

        var hotels = new List<Hotel>
        {
            new Hotel { Id = 1, Name = "Grand Plaza", Country = "USA", City = "New York", Address = "1 Grand St", Description = "Luxury stay", ImagePath = "/images/hotels/plaza.jpg", StarRating = 5, ReviewScore = 9.2, PricePerNight = 350.00m },
            new Hotel { Id = 2, Name = "Beachside Inn", Country = "USA", City = "Miami", Address = "1 Beach Rd", Description = "Ocean views", ImagePath = "/images/hotels/beach.jpg", StarRating = 4, ReviewScore = 8.5, PricePerNight = 200.00m },
            new Hotel { Id = 3, Name = "Mountain Retreat", Country = "USA", City = "Denver", Address = "1 Mountain Way", Description = "Ski access", ImagePath = "/images/hotels/mountain.jpg", StarRating = 4, ReviewScore = 8.8, PricePerNight = 180.00m },
            new Hotel { Id = 4, Name = "Budget Friendly", Country = "USA", City = "Austin", Address = "10 Lowcost St", Description = "Value stay", ImagePath = "/images/hotels/budget.jpg", StarRating = 3, ReviewScore = 7.0, PricePerNight = 80.00m }
        };
        _dbContext.Hotels.AddRange(hotels);
        _dbContext.SaveChanges();
    }

    [Test]
    public async Task GetFeaturedHotelsAsync_ShouldReturnTopRatedHotelsUpToCount()
    {
        int count = 2;
        var result = await _hotelService.GetFeaturedHotelsAsync(count);

        Assert.IsNotNull(result);
        Assert.AreEqual(count, result.Count());
        var orderedExpected = _dbContext.Hotels
                                .OrderByDescending(h => h.StarRating)
                                .ThenByDescending(h => h.ReviewScore)
                                .Take(count)
                                .ToList();
        Assert.AreEqual(orderedExpected[0].Name, result.First().HotelName);
        Assert.AreEqual(orderedExpected[1].Name, result.Last().HotelName);
    }

    [Test]
    public async Task AddHotelAsync_WithValidData_ShouldAddHotelAndReturnTrue()
    {
        var addHotelViewModel = new Aryaans_Hotel_Booking.Models.AddHotelViewModel
        {
            Name = "City Central Hotel",
            Country = "USA",
            City = "Chicago",
            Address = "123 Main St",
            Description = "Modern hotel in downtown Chicago.",
            StarRating = 4,
            PricePerNight = 220.00m,
            ReviewScore = 8.0m
        };

        var mockImageFile = new Mock<IFormFile>();
        var fileName = "chicago_hotel.jpg";
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.Write("Dummy image content");
        writer.Flush();
        stream.Position = 0;
        mockImageFile.Setup(f => f.FileName).Returns(fileName);
        mockImageFile.Setup(f => f.Length).Returns(stream.Length);
        mockImageFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockImageFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
        addHotelViewModel.Image = mockImageFile.Object;

        var success = await _hotelService.AddHotelAsync(addHotelViewModel);

        Assert.IsTrue(success);
        var hotelInDb = await _dbContext.Hotels.FirstOrDefaultAsync(h => h.Name == addHotelViewModel.Name);
        Assert.IsNotNull(hotelInDb);
        Assert.AreEqual(addHotelViewModel.City, hotelInDb.City);
        Assert.IsNotNull(hotelInDb.ImagePath);
        Assert.IsTrue(hotelInDb.ImagePath.Contains(fileName) || hotelInDb.ImagePath.Contains(Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task AddHotelAsync_ImageSaveFails_ShouldReturnFalse()
    {
        var addHotelViewModel = new Aryaans_Hotel_Booking.Models.AddHotelViewModel
        {
            Name = "Faulty Image Hotel",
            Country = "USA",
            City = "FailCity",
            Address = "1 Error Lane",
            Description = "This hotel addition should fail.",
            StarRating = 1,
            PricePerNight = 50.00m
        };
        var mockImageFile = new Mock<IFormFile>();
        mockImageFile.Setup(f => f.FileName).Returns("fail.jpg");
        mockImageFile.Setup(f => f.Length).Returns(100);
        mockImageFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        mockImageFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                     .ThrowsAsync(new IOException("Disk full"));
        addHotelViewModel.Image = mockImageFile.Object;

        _mockWebHostEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

        var result = await _hotelService.AddHotelAsync(addHotelViewModel);
        Assert.IsFalse(result);
    }

    [Test]
    public async Task SearchHotelsAsync_ByKnownCity_ShouldReturnMatchingHotels()
    {
        string destinationQuery = "New York";
        var searchResults = await _hotelService.SearchHotelsAsync(destinationQuery, null, null, null, null, null, "name", "asc", 1, 10);

        Assert.IsNotNull(searchResults);
        Assert.IsNotNull(searchResults.Results);
        Assert.IsTrue(searchResults.Results.Any());
        Assert.IsTrue(searchResults.Results.All(h => h.LocationName.Contains(destinationQuery)));
        Assert.IsTrue(searchResults.Results.Any(h => h.HotelName == "Grand Plaza"));
    }

    [Test]
    public async Task SearchHotelsAsync_WithMinRating_ShouldFilterCorrectly()
    {
        var searchResults = await _hotelService.SearchHotelsAsync(null, null, null, 5, null, null, "name", "asc", 1, 10);
        Assert.IsNotNull(searchResults.Results);
        Assert.IsTrue(searchResults.Results.All(h => h.StarRating >= 5));
        Assert.AreEqual(1, searchResults.Results.Count(h => h.HotelName == "Grand Plaza"));
    }

    [Test]
    public async Task SearchHotelsAsync_WithPriceRange_ShouldFilterCorrectly()
    {
        var searchResults = await _hotelService.SearchHotelsAsync(null, null, null, null, 150m, 250m, "name", "asc", 1, 10);
        Assert.IsNotNull(searchResults.Results);
        Assert.IsTrue(searchResults.Results.Any());
        Assert.IsTrue(searchResults.Results.All(h => h.PricePerNight >= 150m && h.PricePerNight <= 250m));
    }

    [Test]
    public async Task SearchHotelsAsync_SortByPriceAsc_ShouldSortCorrectly()
    {
        var searchResults = await _hotelService.SearchHotelsAsync(null, null, null, null, null, null, "price", "asc", 1, 10);
        Assert.IsNotNull(searchResults.Results);
        Assert.IsTrue(searchResults.Results.Any());
        var prices = searchResults.Results.Select(h => h.PricePerNight).ToList();
        CollectionAssert.IsOrdered(prices, Comparer<decimal>.Default);
    }

    [Test]
    public async Task GetHotelForEditAsync_ExistingHotelId_ShouldReturnViewModel()
    {
        int existingHotelId = 1;
        var result = await _hotelService.GetHotelForEditAsync(existingHotelId);

        Assert.IsNotNull(result);
        Assert.AreEqual(existingHotelId, result.Id);
        Assert.AreEqual("Grand Plaza", result.Name);
        Assert.AreEqual("New York", result.City);
    }

    [Test]
    public async Task GetHotelForEditAsync_NonExistingHotelId_ShouldReturnNull()
    {
        int nonExistingHotelId = 999;
        var result = await _hotelService.GetHotelForEditAsync(nonExistingHotelId);
        Assert.IsNull(result);
    }

    [Test]
    public async Task UpdateHotelAsync_WithValidDataAndNoNewImage_ShouldUpdateHotelAndReturnTrue()
    {
        var editHotelViewModel = new Aryaans_Hotel_Booking.Models.EditHotelViewModel
        {
            Id = 1,
            Name = "Grand Plaza Renovated",
            Country = "USA",
            City = "New York Central",
            Address = "2 Grand St",
            Description = "Newly renovated luxury stay",
            StarRating = 5,
            PricePerNight = 375.00m,
            ReviewScore = 9.3m,
            ExistingImagePath = "/images/hotels/plaza.jpg"
        };

        var success = await _hotelService.UpdateHotelAsync(editHotelViewModel);
        Assert.IsTrue(success);

        var updatedHotel = await _dbContext.Hotels.FindAsync(1);
        Assert.IsNotNull(updatedHotel);
        Assert.AreEqual("Grand Plaza Renovated", updatedHotel.Name);
        Assert.AreEqual("New York Central", updatedHotel.City);
        Assert.AreEqual("/images/hotels/plaza.jpg", updatedHotel.ImagePath);
    }

    [Test]
    public async Task UpdateHotelAsync_WithNewImage_ShouldUpdateHotelAndImageAndReturnTrue()
    {
        var editHotelViewModel = new Aryaans_Hotel_Booking.Models.EditHotelViewModel
        {
            Id = 1,
            Name = "Grand Plaza Deluxe",
            Country = "USA",
            City = "New York",
            Address = "1 Grand St",
            Description = "Luxury stay enhanced",
            StarRating = 5,
            PricePerNight = 400.00m,
            ReviewScore = 9.4m,
            ExistingImagePath = "/images/hotels/plaza.jpg"
        };

        var mockNewImageFile = new Mock<IFormFile>();
        var newFileName = "plaza_deluxe.jpg";
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.Write("New dummy image content");
        writer.Flush();
        stream.Position = 0;
        mockNewImageFile.Setup(f => f.FileName).Returns(newFileName);
        mockNewImageFile.Setup(f => f.Length).Returns(stream.Length);
        mockNewImageFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockNewImageFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
        editHotelViewModel.NewImage = mockNewImageFile.Object;

        var success = await _hotelService.UpdateHotelAsync(editHotelViewModel);
        Assert.IsTrue(success);

        var updatedHotel = await _dbContext.Hotels.FindAsync(1);
        Assert.IsNotNull(updatedHotel);
        Assert.AreEqual("Grand Plaza Deluxe", updatedHotel.Name);
        Assert.IsNotNull(updatedHotel.ImagePath);
        Assert.IsTrue(updatedHotel.ImagePath.Contains(newFileName) || updatedHotel.ImagePath.Contains(updatedHotel.Id.ToString()));
    }

    [Test]
    public async Task UpdateHotelAsync_NonExistingHotel_ShouldReturnFalse()
    {
        var editHotelViewModel = new Aryaans_Hotel_Booking.Models.EditHotelViewModel { Id = 999, Name = "NonExistent", Country = "NA", City = "NA", PricePerNight = 1, StarRating = 1 };
        var result = await _hotelService.UpdateHotelAsync(editHotelViewModel);
        Assert.IsFalse(result);
    }

    [Test]
    public async Task GetHotelForDeletionAsync_ExistingHotelId_ShouldReturnHotel()
    {
        var hotel = await _hotelService.GetHotelForDeletionAsync(1);
        Assert.IsNotNull(hotel);
        Assert.AreEqual(1, hotel.Id);
    }

    [Test]
    public async Task GetHotelForDeletionAsync_NonExistingHotelId_ShouldReturnNull()
    {
        var hotel = await _hotelService.GetHotelForDeletionAsync(999);
        Assert.IsNull(hotel);
    }

    [Test]
    public async Task DeleteHotelAsync_ExistingHotelIdNoBookings_ShouldRemoveHotelAndReturnTrue()
    {
        int hotelIdToDelete = 2;
        var result = await _hotelService.DeleteHotelAsync(hotelIdToDelete);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.OldImagePath);
        var hotelExistsAfterDelete = await _dbContext.Hotels.AnyAsync(h => h.Id == hotelIdToDelete);
        Assert.IsFalse(hotelExistsAfterDelete);
    }

    //[Test]
    //public async Task DeleteHotelAsync_HotelWithBookings_ShouldReturnFalseAndNotDelete()
    //{
    //    var hotelWithBooking = new Hotel { Id = 5, Name = "Booked Hotel", Country = "USA", City = "BookingCity", Address = "1 Booked St", PricePerNight = 100m, StarRating = 3 };
    //    _dbContext.Hotels.Add(hotelWithBooking);
    //    var roomForBooking = new Room { Id = 10, HotelId = 5, RoomNumber = "R1", RoomType = "Standard", PricePerNight = 100m, Capacity = 2, IsAvailable = true };
    //    _dbContext.Rooms.Add(roomForBooking);
    //    _dbContext.Bookings.Add(new Aryaans_Hotel_Booking.Data.Entities.Booking { Id = Guid.NewGuid().ToString(), HotelId = 5, UserId = "testuser", RoomId = 10, CheckInDate = DateTime.UtcNow, CheckOutDate = DateTime.UtcNow.AddDays(1), NumberOfGuests = 1, TotalPrice = 100m, BookingDate = DateTime.UtcNow });
    //    await _dbContext.SaveChangesAsync();

    //    var result = await _hotelService.DeleteHotelAsync(5);
    //    Assert.IsFalse(result.Success);

    //    var hotelStillExists = await _dbContext.Hotels.FindAsync(5);
    //    Assert.IsNotNull(hotelStillExists);
    //}

    [Test]
    public async Task DeleteHotelAsync_NonExistingHotelId_ShouldReturnFalse()
    {
        int nonExistingHotelId = 999;
        var result = await _hotelService.DeleteHotelAsync(nonExistingHotelId);
        Assert.IsFalse(result.Success);
        Assert.IsNull(result.OldImagePath);
    }

    [Test]
    public async Task SaveImageAsync_ValidFile_ShouldSaveAndReturnVirtualPath()
    {
        var mockImageFile = new Mock<IFormFile>();
        var fileName = "valid_image.png";
        using var stream = new MemoryStream(new byte[100]);
        mockImageFile.Setup(f => f.FileName).Returns(fileName);
        mockImageFile.Setup(f => f.Length).Returns(100);
        mockImageFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockImageFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);

        var path = await _hotelService.SaveImageAsync(mockImageFile.Object);
        Assert.IsNotNull(path);
        Assert.IsTrue(path.StartsWith("/images/hotels/"));
        Assert.IsTrue(path.EndsWith(fileName));
    }

    [Test]
    public async Task SaveImageAsync_NullFile_ShouldReturnNull()
    {
        var path = await _hotelService.SaveImageAsync(null!); 
        Assert.IsNull(path);
    }

    [Test]
    public void DeleteImage_ExistingImagePath_ShouldAttemptToDelete()
    {
        string tempFileName = Guid.NewGuid().ToString() + ".jpg";
        string virtualPath = $"/images/hotels/{tempFileName}";
        string physicalDirectory = Path.Combine(_mockWebHostEnvironment.Object.WebRootPath, "images", "hotels");
        Directory.CreateDirectory(physicalDirectory);
        string physicalPath = Path.Combine(physicalDirectory, tempFileName);
        File.WriteAllText(physicalPath, "dummy content");
        Assert.IsTrue(File.Exists(physicalPath));

        _hotelService.DeleteImage(virtualPath);

        Assert.IsFalse(File.Exists(physicalPath));
    }

    [Test]
    public void DeleteImage_NullOrEmptyPath_ShouldNotThrow()
    {
        Assert.DoesNotThrow(() => _hotelService.DeleteImage(null));
        Assert.DoesNotThrow(() => _hotelService.DeleteImage(string.Empty));
    }

    [Test]
    public async Task GetAllHotelsForApiAsync_ShouldReturnAllHotels()
    {
        var result = await _hotelService.GetAllHotelsForApiAsync();
        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.Count());
    }

    [Test]
    public async Task GetHotelByIdForApiAsync_ExistingId_ShouldReturnHotel()
    {
        var result = await _hotelService.GetHotelByIdForApiAsync(1);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Grand Plaza", result.Name);
    }

    [Test]
    public async Task GetHotelByIdForApiAsync_NonExistingId_ShouldReturnNull()
    {
        var result = await _hotelService.GetHotelByIdForApiAsync(999);
        Assert.IsNull(result);
    }

    [Test]
    public async Task PrepareAddHotelViewModelAsync_ShouldReturnNewModel()
    {
        var result = await _hotelService.PrepareAddHotelViewModelAsync();
        Assert.IsNotNull(result);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}