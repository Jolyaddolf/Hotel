using System;

namespace HotelBooking.ViewModels;

public class BusyRoomViewModel
{
    public int? BookingId { get; set; }
    public string? RoomNumber { get; set; }
    public DateOnly? CheckoutDate { get; set; }
    public string? ClientName { get; set; }
    public string? Status { get; set; }
    public DateTime? DisplayCheckoutDate { get; set; }
}