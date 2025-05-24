using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using HotelBooking.Context;
using HotelBooking.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBooking
{
    public class BookingHistoryViewModel
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private readonly User001Context _context;
        private ObservableCollection<Client> _clients = new();
        private ObservableCollection<AvailableRoomsToday> _availableRooms = new();
        private ObservableCollection<BusyRoomViewModel> _busyRooms = new();

        public MainWindow()
        {
            _context = new User001Context();
            InitializeComponent();
            LoadDataAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LoadDataAsync()
        {
            await LoadClients();
            await LoadAvailableRooms();
            await LoadBusyRooms();
            BindControls();
        }

        private void BindControls()
        {
            this.FindControl<ListBox>("ListClients").ItemsSource = _clients;
            this.FindControl<ListBox>("ListAvailableRooms").ItemsSource = _availableRooms;
            this.FindControl<ListBox>("ListBusyRooms").ItemsSource = _busyRooms;
        }

        private async Task LoadClients(string searchText = "")
        {
            _clients.Clear();
            var query = _context.Clients.AsQueryable();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(c => c.FullName.Contains(searchText) || c.Phone.Contains(searchText));
            }
            var clients = await query.ToListAsync();
            foreach (var client in clients)
            {
                _clients.Add(client);
            }
            Console.WriteLine($"Loaded {clients.Count} clients");
        }

        private async Task LoadAvailableRooms(bool ascending = true)
        {
            _availableRooms.Clear();
            var query = _context.AvailableRoomsTodays.AsQueryable();
            query = ascending ? query.OrderBy(r => r.PricePerNight) : query.OrderByDescending(r => r.PricePerNight);
            var rooms = await query.ToListAsync();
            foreach (var room in rooms)
            {
                _availableRooms.Add(room);
            }
            Console.WriteLine($"Loaded {rooms.Count} available rooms: {string.Join(", ", rooms.Select(r => r.Number))}");
        }

        private async Task LoadBusyRooms(string sort = "CheckoutDate")
        {
            _busyRooms.Clear();
            var query = _context.BusyRoomsTodays.AsQueryable();
            query = sort == "CheckoutDate" ? query.OrderBy(b => b.CheckoutDate) : query.OrderBy(b => b.ClientName);
            var bookings = await query.ToListAsync();
            foreach (var booking in bookings)
            {
                var viewModel = new BusyRoomViewModel
                {
                    BookingId = booking.BookingId,
                    RoomNumber = booking.RoomNumber,
                    CheckoutDate = booking.CheckoutDate,
                    ClientName = booking.ClientName,
                    Status = booking.Status.ToString(),
                    DisplayCheckoutDate = booking.CheckoutDate.HasValue
                        ? booking.CheckoutDate.Value.ToDateTime(TimeOnly.MinValue)
                        : null
                };
                _busyRooms.Add(viewModel);
            }
            Console.WriteLine($"Loaded {bookings.Count} busy rooms: {string.Join(", ", bookings.Select(b => $"Room {b.RoomNumber} (Client: {b.ClientName}, Checkout: {b.CheckoutDate})"))}");
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchBox = sender as TextBox;
            if (searchBox != null)
            {
                var searchText = searchBox.Text;
                await Task.Delay(300);
                await LoadClients(searchText);
            }
        }

        private async void ListClients_DoubleTapped(object sender, TappedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is Client client)
            {
                var fullNameTextBox = new TextBox { Text = client.FullName, Name = "FullName" };
                var phoneTextBox = new TextBox { Text = client.Phone, Name = "Phone" };
                var emailTextBox = new TextBox { Text = client.Email, Name = "Email" };
                var passportTextBox = new TextBox { Text = client.Passport, Name = "Passport" };
                var saveButton = new Button { Content = "Сохранить", Name = "SaveButton" };

                var dialog = new Window
                {
                    Title = "Редактирование клиента",
                    Width = 400,
                    Height = 300,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(10),
                        Children =
                        {
                            fullNameTextBox,
                            phoneTextBox,
                            emailTextBox,
                            passportTextBox,
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Children = { saveButton }
                            }
                        }
                    }
                };

                saveButton.Click += async (_, __) =>
                {
                    var fullName = fullNameTextBox.Text;
                    var phone = phoneTextBox.Text;
                    if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
                    {
                        await new Window
                        {
                            Title = "Ошибка",
                            Content = new TextBlock { Text = "ФИО и телефон обязательны!", Margin = new Thickness(10) },
                            Width = 200,
                            Height = 100
                        }.ShowDialog(dialog);
                        return;
                    }
                    client.FullName = fullName;
                    client.Phone = phone;
                    client.Email = emailTextBox.Text;
                    client.Passport = passportTextBox.Text;
                    _context.Clients.Update(client);
                    await _context.SaveChangesAsync();
                    await LoadClients();
                    await LoadBusyRooms(); // Обновляем список занятых номеров
                    dialog.Close();
                };

                await dialog.ShowDialog(this);
            }
        }

        private async void ListBusyRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deleteButton = this.FindControl<Button>("DeleteBookingButton");
            deleteButton.IsEnabled = (sender as ListBox)?.SelectedItem != null;
        }

        private async void ListBusyRooms_DoubleTapped(object sender, TappedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is BusyRoomViewModel busyRoom && busyRoom.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == busyRoom.BookingId.Value);
                if (booking != null)
                {
                    var dialog = new RoomDetailsDialog(busyRoom, booking);
                    await dialog.ShowDialog(this);
                }
            }
        }

        private async void ListAvailableRooms_DoubleTapped(object sender, TappedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is AvailableRoomsToday availableRoom)
            {
                var dialog = new RoomDetailsDialog(availableRoom);
                await dialog.ShowDialog(this);
            }
        }

        private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = this.FindControl<ListBox>("ListClients").SelectedItem as Client;
            if (selectedClient == null)
            {
                await new Window
                {
                    Title = "Ошибка",
                    Content = new TextBlock { Text = "Выберите клиента!", Margin = new Thickness(10) },
                    Width = 200,
                    Height = 100
                }.ShowDialog(this);
                return;
            }

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.ClientId == selectedClient.Id)
                .Select(b => new BookingHistoryViewModel
                {
                    Id = b.Id,
                    RoomNumber = b.Room.Number,
                    StartDate = b.StartDate.ToDateTime(TimeOnly.MinValue),
                    EndDate = b.EndDate.ToDateTime(TimeOnly.MinValue),
                    Status = b.Status.ToString()
                })
                .ToListAsync();

            var dialog = new Window
            {
                Title = $"История бронирования: {selectedClient.FullName}",
                Width = 600,
                Height = 400,
                Content = new ListBox
                {
                    ItemsSource = bookings,
                    ItemTemplate = new FuncDataTemplate<BookingHistoryViewModel>((item, _) =>
                    {
                        var grid = new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitions("100,120,120,100")
                        };
                        grid.Children.Add(new TextBlock
                        {
                            [Grid.ColumnProperty] = 0,
                            [!TextBlock.TextProperty] = new Binding("RoomNumber"),
                            Margin = new Thickness(2)
                        });
                        grid.Children.Add(new TextBlock
                        {
                            [Grid.ColumnProperty] = 1,
                            [!TextBlock.TextProperty] = new Binding("StartDate") { StringFormat = "dd.MM.yyyy" },
                            Margin = new Thickness(2)
                        });
                        grid.Children.Add(new TextBlock
                        {
                            [Grid.ColumnProperty] = 2,
                            [!TextBlock.TextProperty] = new Binding("EndDate") { StringFormat = "dd.MM.yyyy" },
                            Margin = new Thickness(2)
                        });
                        grid.Children.Add(new TextBlock
                        {
                            [Grid.ColumnProperty] = 3,
                            [!TextBlock.TextProperty] = new Binding("Status"),
                            Margin = new Thickness(2)
                        });
                        return grid;
                    }, true)
                }
            };
            await dialog.ShowDialog(this);
        }

        private void CreateBookingButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dialog = new CreateBookingDialog(_context, async () =>
            {
                await LoadAvailableRooms();
                await LoadBusyRooms();
            });
            dialog.Show();
        }

        private async void DeleteBookingButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBooking = this.FindControl<ListBox>("ListBusyRooms").SelectedItem as BusyRoomViewModel;
            if (selectedBooking == null || selectedBooking.BookingId == null)
            {
                await new Window
                {
                    Title = "Ошибка",
                    Content = new TextBlock { Text = "Выберите бронь!", Margin = new Thickness(10) },
                    Width = 200,
                    Height = 100
                }.ShowDialog(this);
                return;
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == selectedBooking.BookingId.Value);
            if (booking != null)
            {
                booking.Status = BookingStatus.Canceled;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await LoadAvailableRooms();
                await LoadBusyRooms();
                Console.WriteLine($"Booking {booking.Id} canceled successfully");
            }
        }

        private async void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            var fullNameTextBox = new TextBox { Name = "FullName", Watermark = "ФИО" };
            var phoneTextBox = new TextBox { Name = "Phone", Watermark = "Телефон" };
            var emailTextBox = new TextBox { Name = "Email", Watermark = "Email (опционально)" };
            var passportTextBox = new TextBox { Name = "Passport", Watermark = "Паспорт (опционально)" };
            var saveButton = new Button { Content = "Сохранить", Name = "SaveClientButton" };

            var dialog = new Window
            {
                Title = "Добавить клиента",
                Width = 400,
                Height = 300,
                Content = new StackPanel
                {
                    Margin = new Thickness(10),
                    Children =
                    {
                        fullNameTextBox,
                        phoneTextBox,
                        emailTextBox,
                        passportTextBox,
                        saveButton
                    }
                }
            };

            saveButton.Click += async (_, __) =>
            {
                var fullName = fullNameTextBox.Text;
                var phone = phoneTextBox.Text;
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
                {
                    await new Window
                    {
                        Title = "Ошибка",
                        Content = new TextBlock { Text = "ФИО и телефон обязательны!", Margin = new Thickness(10) },
                        Width = 200,
                        Height = 100
                    }.ShowDialog(dialog);
                    return;
                }

                var client = new Client
                {
                    FullName = fullName,
                    Phone = phone,
                    Email = emailTextBox.Text,
                    Passport = passportTextBox.Text
                };
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                await LoadClients();
                await LoadBusyRooms(); // Обновляем занятые номера после добавления клиента
                dialog.Close();
            };

            await dialog.ShowDialog(this);
        }

        private async void DeleteClientButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = this.FindControl<ListBox>("ListClients").SelectedItem as Client;
            if (selectedClient == null)
            {
                await new Window
                {
                    Title = "Ошибка",
                    Content = new TextBlock { Text = "Выберите клиента!", Margin = new Thickness(10) },
                    Width = 200,
                    Height = 100
                }.ShowDialog(this);
                return;
            }

            var activeBookings = await _context.Bookings
                .CountAsync(b => b.ClientId == selectedClient.Id && b.Status == BookingStatus.Booked);
            if (activeBookings > 0)
            {
                await new Window
                {
                    Title = "Ошибка",
                    Content = new TextBlock { Text = "У клиента есть активные брони!", Margin = new Thickness(10) },
                    Width = 200,
                    Height = 100
                }.ShowDialog(this);
                return;
            }

            _context.Clients.Remove(selectedClient);
            await _context.SaveChangesAsync();
            await LoadClients();
            await LoadBusyRooms(); // Обновляем занятые номера после удаления клиента
        }

        private async void SortAvailableRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox.SelectedIndex >= 0)
            {
                var ascending = comboBox.SelectedIndex == 0;
                await LoadAvailableRooms(ascending);
            }
        }

        private async void SortBusyRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox.SelectedIndex >= 0)
            {
                var sort = comboBox.SelectedIndex == 0 ? "CheckoutDate" : "ClientName";
                await LoadBusyRooms(sort);
            }
        }
    }

    public class BusyRoomViewModel
    {
        public int? BookingId { get; set; }
        public string? RoomNumber { get; set; }
        public DateOnly? CheckoutDate { get; set; }
        public string? ClientName { get; set; }
        public string? Status { get; set; }
        public DateTime? DisplayCheckoutDate { get; set; }
    }
}