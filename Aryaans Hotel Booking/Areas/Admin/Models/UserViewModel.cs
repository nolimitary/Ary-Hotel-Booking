﻿using System.Collections.Generic;

namespace Aryaans_Hotel_Booking.Areas.Admin.Models
{
    public class UserViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
