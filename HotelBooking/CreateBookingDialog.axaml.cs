using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HotelBooking.Context;
using HotelBooking.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBooking
{
    public partial class CreateBookingDialog : Window
    {
        private User001Context _context;
        private Action _onBookingCreated;
        private List<AvailableRoomsToday> _rooms;
        private List<Client> _clients;

        public CreateBookingDialog()
        {
            InitializeComponent();
        }

        public CreateBookingDialog(User001Context context, Action onBookingCreated) : this()
        {
            _context = context;
            _onBookingCreated = onBookingCreated;
            InitializeData();
        }

        private void InitializeComponent()
        {
            Console.WriteLine("Loading XAML for CreateBookingDialog...");
            AvaloniaXamlLoader.Load(this);
            Console.WriteLine("XAML loaded successfully");
        }

        private async void InitializeData()
        {
            Console.WriteLine("Initializing CreateBookingDialog data...");
            if (_context == null)
            {
                throw new InvalidOperationException("Context is not initialized. Call InitializeContext before showing the dialog.");
            }

            _rooms = await _context.AvailableRoomsTodays.OrderBy(r => r.PricePerNight).ToListAsync();
            this.FindControl<ComboBox>("RoomComboBox").ItemsSource = _rooms;
            Console.WriteLine($"Loaded {_rooms.Count} rooms");

            _clients = await _context.Clients.OrderBy(c => c.FullName).ToListAsync();
            this.FindControl<ComboBox>("ClientComboBox").ItemsSource = _clients;
            Console.WriteLine($"Loaded {_clients.Count} clients");

            var startDatePicker = this.FindControl<DatePicker>("StartDatePicker");
            startDatePicker.SelectedDate = DateTime.Today;
            Console.WriteLine($"StartDatePicker set to: {startDatePicker.SelectedDate}");

            var endDatePicker = this.FindControl<DatePicker>("EndDatePicker");
            endDatePicker.SelectedDate = DateTime.Today.AddDays(1);
            Console.WriteLine($"EndDatePicker set to: {endDatePicker.SelectedDate}");

            this.FindControl<Button>("CalculateButton").Click += CalculateButton_Click;
            this.FindControl<Button>("SaveButton").Click += async (s, e) => await SaveButton_Click(s, e);

            this.FindControl<ComboBox>("RoomComboBox").SelectionChanged += (s, e) => UpdateTotalCost();
            this.FindControl<DatePicker>("StartDatePicker").SelectedDateChanged += (s, e) => UpdateTotalCost();
            this.FindControl<DatePicker>("EndDatePicker").SelectedDateChanged += (s, e) => UpdateTotalCost();
        }

        public void InitializeContext(User001Context context, Action onBookingCreated)
        {
            _context = context;
            _onBookingCreated = onBookingCreated;
            InitializeData();
        }

        private void CalculateButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateTotalCost();
        }

        private void UpdateTotalCost()
        {
            var room = this.FindControl<ComboBox>("RoomComboBox").SelectedItem as AvailableRoomsToday;
            var startDate = this.FindControl<DatePicker>("StartDatePicker").SelectedDate;
            var endDate = this.FindControl<DatePicker>("EndDatePicker").SelectedDate;

            if (room != null && startDate.HasValue && endDate.HasValue && startDate.Value.Date <= endDate.Value.Date)
            {
                var nights = (endDate.Value.Date - startDate.Value.Date).Days;
                var totalCost = room.PricePerNight * nights;
                this.FindControl<TextBlock>("TotalCostText").Text = $"����� ���������: {totalCost:F2} ���.";
            }
            else
            {
                this.FindControl<TextBlock>("TotalCostText").Text = "����� ���������: 0 ���.";
            }
        }

        private async Task SaveButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var room = this.FindControl<ComboBox>("RoomComboBox").SelectedItem as AvailableRoomsToday;
            var client = this.FindControl<ComboBox>("ClientComboBox").SelectedItem as Client;
            var startDate = this.FindControl<DatePicker>("StartDatePicker").SelectedDate;
            var endDate = this.FindControl<DatePicker>("EndDatePicker").SelectedDate;

            if (room == null || client == null || !startDate.HasValue || !endDate.HasValue)
            {
                await ShowErrorDialog("��������� ��� ����!");
                return;
            }

            if (startDate.Value.Date < DateTime.Today)
            {
                await ShowErrorDialog("���� ������ �� ����� ���� � �������!");
                return;
            }

            if (startDate.Value.Date >= endDate.Value.Date)
            {
                await ShowErrorDialog("���� ������ ������ ���� ����� ���� ������!");
                return;
            }

            var booking = new Booking
            {
                ClientId = client.Id,
                RoomId = room.Id.Value,
                StartDate = DateOnly.FromDateTime(startDate.Value.Date),
                EndDate = DateOnly.FromDateTime(endDate.Value.Date),
                CreatedAt = DateTime.UtcNow,
                Status = BookingStatus.Booked
            };

            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                _onBookingCreated?.Invoke();
                Close();
            }
            catch (DbUpdateException ex)
            {
                await ShowErrorDialog($"�������� ������������: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            await new Window
            {
                Title = "���������",
                Content = new TextBlock { Text = message, Margin = new Thickness(10) },
                Width = 300,
                Height = 100
            }.ShowDialog(this);
        }

        private async void GenerateReceiptButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var client = this.FindControl<ComboBox>("ClientComboBox").SelectedItem as Client;
            var room = this.FindControl<ComboBox>("RoomComboBox").SelectedItem as AvailableRoomsToday;
            var totalCostText = this.FindControl<TextBlock>("TotalCostText").Text;
            var totalCost = decimal.Parse(totalCostText.Replace("����� ���������: ", "").Replace(" ���.", ""));

            if (client == null || room == null)
            {
                await ShowErrorDialog("�������� ������� � ����� ����� ������������� ����!");
                return;
            }

            // ������������ ����������� ����
            var receiptContent = $"��� �� ������������\n" +
                               $"���� � �����: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n" +
                               $"������: {client.FullName}\n" +
                               $"�����: {room.Number}\n" +
                               $"����� ���������: {totalCost:F2} ���.";

            // ���� � �������� �����
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var receiptsFolder = Path.Combine(desktopPath, "����");

            // �������� ����� "����", ���� ��� �� ����������
            if (!Directory.Exists(receiptsFolder))
            {
                Directory.CreateDirectory(receiptsFolder);
            }

            // ������������ ����� ����� � ����
            var fileName = $"Receipt_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(receiptsFolder, fileName);

            // ���������� �����
            File.WriteAllText(filePath, receiptContent);
            await ShowErrorDialog($"��� ������� � ����� ���� �� ������� �����: {fileName}");
        }
    }
}