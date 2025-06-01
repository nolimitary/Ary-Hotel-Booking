using Aryaans_Hotel_Booking.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Aryaans_Hotel_Booking.Data.Entities;

namespace Aryaans_Hotel_Booking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HotelApiController> _logger;

        public HotelApiController(ApplicationDbContext context, ILogger<HotelApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any, NoStore = false)] // Cache for 600 seconds (10 minutes)
        public async Task<ActionResult<IEnumerable<Hotel>>> GetHotels()
        {
            _logger.LogInformation("API: GetHotels called.");
            try
            {
                var hotels = await _context.Hotels.ToListAsync();
                if (hotels == null || !hotels.Any())
                {
                    _logger.LogWarning("API: GetHotels - No hotels found.");
                    return NotFound("No hotels found.");
                }
                return Ok(hotels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: GetHotels - An error occurred while fetching hotels.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("{id}")]
        [ResponseCache(Duration = 1200, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "id" })] // Cache specific hotel for 20 minutes
        public async Task<ActionResult<Hotel>> GetHotel(int id)
        {
            _logger.LogInformation($"API: GetHotel called with ID: {id}.");
            try
            {
                var hotel = await _context.Hotels.FindAsync(id);

                if (hotel == null)
                {
                    _logger.LogWarning($"API: GetHotel - Hotel with ID {id} not found.");
                    return NotFound($"Hotel with ID {id} not found.");
                }

                return Ok(hotel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API: GetHotel - An error occurred while fetching hotel with ID {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}