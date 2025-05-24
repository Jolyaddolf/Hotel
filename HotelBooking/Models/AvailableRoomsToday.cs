using System;
using System.Collections.Generic;

namespace HotelBooking.Models;

public partial class AvailableRoomsToday
{
    public int? Id { get; set; }

    public string? Number { get; set; }

    public string? Description { get; set; }

    public string? Style { get; set; }

    public int? Capacity { get; set; }

    public decimal? PricePerNight { get; set; }

    public bool? IsActive { get; set; }
}
