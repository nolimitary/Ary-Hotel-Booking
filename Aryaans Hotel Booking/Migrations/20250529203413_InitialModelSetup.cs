using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Aryaans_Hotel_Booking.Migrations
{
    /// <inheritdoc />
    public partial class InitialModelSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Hotels",
                columns: new[] { "Id", "City", "Country", "ImagePath", "Name", "PricePerNight", "ReviewScore", "StarRating" },
                values: new object[,]
                {
                    { 1, "New York", "USA", "/images/hotels/grand_hyatt.jpg", "Grand Hyatt", 300.00m, 8.8000000000000007, 5 },
                    { 2, "New York", "USA", "/images/hotels/the_plaza.jpg", "The Plaza", 500.00m, 9.1999999999999993, 5 }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "Capacity", "Description", "HotelId", "PricePerNight", "RoomType" },
                values: new object[,]
                {
                    { 1, 2, "Spacious room with a king-sized bed and city view.", 1, 450.00m, "Deluxe King" },
                    { 2, 2, "Comfortable room with a queen-sized bed.", 1, 350.00m, "Standard Queen" },
                    { 3, 4, "Luxurious suite with a separate living area and premium amenities.", 2, 799.99m, "Plaza Suite" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

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
    }
}
