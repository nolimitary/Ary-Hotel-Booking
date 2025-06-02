using Aryaans_Hotel_Booking.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using Aryaans_Hotel_Booking.Data.Entities;

namespace Aryaans_Hotel_Booking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelApiController : ControllerBase
    {
        private readonly IHotelService _hotelService;
        private readonly ILogger<HotelApiController> _logger;

        public HotelApiController(IHotelService hotelService, ILogger<HotelApiController> logger)
        {
            _hotelService = hotelService;
            _logger = logger;
        }

        [HttpGet]
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<ActionResult<IEnumerable<Hotel>>> GetHotels()
        {
            _logger.LogInformation("API: GetHotels called via service.");
            try
            {
                var hotels = await _hotelService.GetAllHotelsForApiAsync();
                if (hotels == null || !hotels.Any())
                {
                    _logger.LogWarning("API: GetHotels - No hotels found via service.");
                    return NotFound("No hotels found.");
                }
                return Ok(hotels);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "API: GetHotels - An error occurred while fetching hotels via service.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("{id}")]
        [ResponseCache(Duration = 1200, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "id" })]
        public async Task<ActionResult<Hotel>> GetHotel(int id)
        {
            _logger.LogInformation($"API: GetHotel called with ID: {id} via service.");
            try
            {
                var hotel = await _hotelService.GetHotelByIdForApiAsync(id);
                if (hotel == null)
                {
                    _logger.LogWarning($"API: GetHotel - Hotel with ID {id} not found via service.");
                    return NotFound($"Hotel with ID {id} not found.");
                }
                return Ok(hotel);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"API: GetHotel - An error occurred while fetching hotel with ID {id} via service.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}