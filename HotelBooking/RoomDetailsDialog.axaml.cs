using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HotelBooking.Models;
using System;

namespace HotelBooking
{
    public partial class RoomDetailsDialog : Window
    {
        private TextBlock _roomNumberText;
        private TextBlock _detail1Text;
        private TextBlock _detail2Text;
        private TextBlock _detail3Text;
        private TextBlock _detail4Text;
        private TextBlock _statusText;

        public RoomDetailsDialog()
        {
            InitializeComponent();
        }

        public RoomDetailsDialog(BusyRoomViewModel busyRoom, Booking booking) : this()
        {
            Title = $"Детали занятого номера: {busyRoom.RoomNumber}";
            UpdateBusyRoomDetails(busyRoom, booking);
        }

        public RoomDetailsDialog(AvailableRoomsToday availableRoom) : this()
        {
            Title = $"Детали свободного номера: {availableRoom.Number}";
            UpdateAvailableRoomDetails(availableRoom);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _roomNumberText = this.FindControl<TextBlock>("RoomNumberText");
            _detail1Text = this.FindControl<TextBlock>("Detail1Text");
            _detail2Text = this.FindControl<TextBlock>("Detail2Text");
            _detail3Text = this.FindControl<TextBlock>("Detail3Text");
            _detail4Text = this.FindControl<TextBlock>("Detail4Text");
            _statusText = this.FindControl<TextBlock>("StatusText");
        }

        private void UpdateBusyRoomDetails(BusyRoomViewModel busyRoom, Booking booking)
        {
            _roomNumberText.Text = $"Номер: {busyRoom.RoomNumber}";
            _detail1Text.Text = busyRoom.ClientName;
            _detail2Text.Text = booking.StartDate.ToString("dd.MM.yyyy");
            _detail3Text.Text = busyRoom.CheckoutDate?.ToString("dd.MM.yyyy") ?? "Не указано";
            _detail4Text.Text = "";

            _statusText.Text = $"Статус: {busyRoom.Status}";
            _statusText.Foreground = busyRoom.Status == "Активен" ?
                this.FindResource("SuccessColor") as SolidColorBrush :
                this.FindResource("DangerColor") as SolidColorBrush;
        }

        private void UpdateAvailableRoomDetails(AvailableRoomsToday availableRoom)
        {
            _roomNumberText.Text = $"Номер: {availableRoom.Number}";
            _detail1Text.Text = availableRoom.Description ?? "Нет описания";
            _detail2Text.Text = availableRoom.Style ?? "Не указан";
            _detail3Text.Text = $"{availableRoom.Capacity} человек";
            _detail4Text.Text = $"{availableRoom.PricePerNight:F2} руб.";

            _statusText.Text = availableRoom.IsActive == true ? "Активен" : "Неактивен";
            _statusText.Foreground = availableRoom.IsActive == true ?
                this.FindResource("SuccessColor") as SolidColorBrush :
                this.FindResource("DangerColor") as SolidColorBrush;
        }
    }
}