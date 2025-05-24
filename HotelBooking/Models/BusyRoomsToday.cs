using System;
using System.Collections.Generic;

namespace HotelBooking.Models;

public partial class BusyRoomsToday
{
    public int? BookingId { get; set; }

    public string? RoomNumber { get; set; }

    public DateOnly? CheckoutDate { get; set; }

    public string? ClientName { get; set; }

    public BookingStatus Status { get; set; } // Изменено на BookingStatus
}