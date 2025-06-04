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
            _detail1Text.Text = booking.Room?.Description ?? "Нет описания"; // Описание номера
            _detail2Text.Text = booking.Room?.Style ?? "Не указан"; // Тип (стиль)
            _detail3Text.Text = $"{booking.Room?.Capacity ?? 0} человек"; // Вместимость
            _detail4Text.Text = $"{booking.Room?.PricePerNight ?? 0:F2} руб."; // Цена за ночь

            // Форматируем статус с переносами
            var clientInfo = $"Забронировал: {busyRoom.ClientName}";
            var dateRange = $"Период: {booking.StartDate:dd.MM.yyyy} - {busyRoom.CheckoutDate?.ToString("dd.MM.yyyy") ?? "Не указано"}";
            var statusInfo = $"Статус: {busyRoom.Status}";
            _statusText.Text = $"{clientInfo}\n{dateRange}\n{statusInfo}";

            // Цвет статуса (зелёный для "Booked", красный для других)
            _statusText.Foreground = busyRoom.Status == BookingStatus.Booked ?
                new SolidColorBrush(Color.Parse("#00a651")) :
                new SolidColorBrush(Color.Parse("#f66b60"));
            
        }

        private void UpdateAvailableRoomDetails(AvailableRoomsToday availableRoom)
        {
            _roomNumberText.Text = $"Номер: {availableRoom.Number}";
            _detail1Text.Text = availableRoom.Description ?? "Нет описания";
            _detail2Text.Text = availableRoom.Style ?? "Не указан";
            _detail3Text.Text = $"{availableRoom.Capacity} человек";
            _detail4Text.Text = $"{availableRoom.PricePerNight:F2} руб.";

            bool isActive = availableRoom.IsActive ?? false;
            _statusText.Text = $"Статус: {(isActive ? "Активен" : "Неактивен")}";
            _statusText.Foreground = isActive ?
                new SolidColorBrush(Color.Parse("#00a651")) :
                new SolidColorBrush(Color.Parse("#f66b60"));
        }
    }
}