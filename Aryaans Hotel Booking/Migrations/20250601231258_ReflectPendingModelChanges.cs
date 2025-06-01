using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Aryaans_Hotel_Booking.Migrations
{
    /// <inheritdoc />
    public partial class ReflectPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Rooms",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Hotels",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Hotels",
                keyColumn: "Id",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Hotels",
                columns: new[] { "Id", "Address", "City", "Country", "Description", "ImagePath", "Name", "PricePerNight", "ReviewScore", "StarRating" },
                values: new object[,]
                {
                    { 1, "123 Luxury Lane", "New York", "USA", "A luxurious hotel in the heart of the city.", "/images/hotels/grand_hyatt.jpg", "Grand Hyatt", 300.00m, 8.8000000000000007, 5 },
                    { 2, "768 Fifth Avenue", "New York", "USA", "Iconic luxury hotel with a rich history.", "/images/hotels/the_plaza.jpg", "The Plaza", 500.00m, 9.1999999999999993, 5 }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "Amenities", "Capacity", "Description", "HotelId", "IsAvailable", "PricePerNight", "RoomNumber", "RoomType" },
                values: new object[,]
                {
                    { 1, "WiFi, TV, AC, Minibar", 2, "Spacious room with a king-sized bed and city view.", 1, true, 450.00m, "101", "Deluxe King" },
                    { 2, "WiFi, TV, AC", 2, "Comfortable room with a queen-sized bed.", 1, false, 350.00m, "102", "Standard Queen" },
                    { 3, "WiFi, TV, AC, Minibar, Jacuzzi", 4, "Luxurious suite with a separate living area and premium amenities.", 2, true, 799.99m, "201", "Plaza Suite" }
                });
        }
    }
}
