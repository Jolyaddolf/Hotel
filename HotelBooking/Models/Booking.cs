using System;
using System.Collections.Generic;

namespace HotelBooking.Models
{

    public enum BookingStatus
    {
        Booked,
        Canceled,
        Completed
    }

    public class Booking
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int RoomId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Booked";

        public virtual Client Client { get; set; }
        public virtual Room Room { get; set; }
    }

}