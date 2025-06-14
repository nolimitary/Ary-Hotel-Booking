﻿using System.Collections.Generic;

namespace Aryaans_Hotel_Booking.Models
{
    public class HotelResultViewModel
    {
        public int Id { get; set; } 
        public string HotelName { get; set; }
        public string ImageUrl { get; set; }
        public int StarRating { get; set; }
        public string LocationName { get; set; }
        public string DistanceFromCenter { get; set; }
        public decimal ReviewScore { get; set; }
        public string ReviewScoreText { get; set; }
        public int ReviewCount { get; set; }
        public decimal PricePerNight { get; set; }
        public string CurrencySymbol { get; set; } = "BGN";
        public string AvailabilityUrl { get; set; }
        public List<RoomInfoViewModel> RecommendedRooms { get; set; } = new List<RoomInfoViewModel>();
    }
}