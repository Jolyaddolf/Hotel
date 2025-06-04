using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HotelBooking.ViewModels;
using HotelBooking.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBooking
{
    public partial class GenerateReportDialog : Window
    {
        private readonly User001Context _context;

        public GenerateReportDialog()
        {
            InitializeComponent();
        }

        public GenerateReportDialog(User001Context context) : this()
        {
            _context = context;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void GenerateButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var monthComboBox = this.FindControl<ComboBox>("MonthComboBox");
            var selectedMonthIndex = monthComboBox.SelectedIndex + 1; // Индекс начинается с 0, месяцы с 1
            if (selectedMonthIndex < 1)
            {
                await ShowErrorDialog("Выберите месяц!");
                return;
            }

            var year = DateTime.Now.Year; // Можно добавить выбор года, если требуется
            var startOfMonth = new DateOnly(year, selectedMonthIndex, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            using (var context = new User001Context())
            {
                var bookings = await context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.Client)
                    .Where(b => b.StartDate >= startOfMonth && b.StartDate <= endOfMonth)
                    .ToListAsync();

                var totalBookings = bookings.Count;
                var uniqueClients = bookings.Select(b => b.ClientId).Distinct().Count();
                var totalRevenue = bookings
                    .Where(b => b.Status == "Booked" || b.Status == "Completed")
                    .Sum(b => b.Room.PricePerNight * (b.EndDate.ToDateTime(TimeOnly.MinValue) - b.StartDate.ToDateTime(TimeOnly.MinValue)).Days);
                var bookedCount = bookings.Count(b => b.Status == "Booked");
                var canceledCount = bookings.Count(b => b.Status == "Canceled");
                var completedCount = bookings.Count(b => b.Status == "Completed");

                var reportContent = $"Отчет за {monthComboBox.SelectedItem} {year}\n" +
                                   $"----------------------------------------\n" +
                                   $"Общее количество бронирований: {totalBookings}\n" +
                                   $"Уникальных клиентов: {uniqueClients}\n" +
                                   $"Общий доход: {totalRevenue:F2} руб.\n" +
                                   $"Статусы бронирований:\n" +
                                   $"  - Забронировано: {bookedCount}\n" +
                                   $"  - Отменено: {canceledCount}\n" +
                                   $"  - Завершено: {completedCount}\n";

                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var reportsFolder = Path.Combine(desktopPath, "Отчеты");
                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                var fileName = $"Report_{year}_{selectedMonthIndex:D2}.txt";
                var filePath = Path.Combine(reportsFolder, fileName);
                File.WriteAllText(filePath, reportContent);

                await ShowErrorDialog($"Отчет сохранен в {filePath}");
                Close();
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            var dialog = new Window
            {
                Title = "Сообщение",
                Width = 300,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#F5F1ED")),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(8),
                    Padding = new Thickness(8),
                    Child = new StackPanel
                    {
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Сообщение",
                                Classes = { "header" },
                                TextAlignment = TextAlignment.Center,
                                Foreground = new SolidColorBrush(Color.Parse("#00a651"))
                            },
                            new TextBlock
                            {
                                Text = message,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 14
                            }
                        }
                    }
                }
            };
            await dialog.ShowDialog(this);
        }
    }
}