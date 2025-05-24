using System;
using System.Collections.Generic;

namespace HotelBooking.Models;
public enum BookingStatus
{
    Booked,
    Canceled,
    Completed
}
public partial class Booking
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public int RoomId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Booked;

    public virtual Client Client { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
