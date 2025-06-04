
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using HotelBooking.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
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
        private readonly object _loadClientsLock = new object();
        private string _lastSearchText = string.Empty;

        public MainWindow()
        {
            _context = new User001Context();
            InitializeComponent();
            LoadDataAsync();
            UpdateBookingStatuses();
            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (searchBox != null)
            {
                searchBox.TextChanged += SearchBox_TextChanged;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LoadDataAsync()
        {
            await UpdateBookingStatuses();
            await LoadClients();
            await LoadAvailableRooms();
            await LoadBusyRooms();
            BindControls();
        }

        private async Task UpdateBookingStatuses()
        {
            using (var context = new User001Context())
            {
                await context.Database.ExecuteSqlRawAsync("CALL boking.update_booking_status()");
            }
        }

        private void BindControls()
        {
            this.FindControl<ListBox>("ListClients").ItemsSource = _clients;
            this.FindControl<ListBox>("ListAvailableRooms").ItemsSource = _availableRooms;
            this.FindControl<ListBox>("ListBusyRooms").ItemsSource = _busyRooms;
        }

        private async Task LoadClients(string searchText = "")
        {
            lock (_loadClientsLock)
            {
                _lastSearchText = searchText;
            }

            using (var context = new User001Context())
            {
                var query = context.Clients.AsQueryable();
                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(c => c.FullName.ToLower().Contains(searchText.ToLower()) ||
                                          c.Phone.ToLower().Contains(searchText.ToLower()) ||
                                          c.Passport.ToLower().Contains(searchText.ToLower()));
                }

                var clients = await query.ToListAsync();

                lock (_loadClientsLock)
                {
                    if (_lastSearchText != searchText)
                    {
                        return;
                    }

                    _clients.Clear();
                    foreach (var client in clients)
                    {
                        _clients.Add(client);
                    }
                }
            }
        }

        private async Task LoadAvailableRooms(bool ascending = true)
        {
            _availableRooms.Clear();
            using (var context = new User001Context())
            {
                var query = context.AvailableRoomsTodays.AsQueryable();
                query = ascending ? query.OrderBy(r => r.PricePerNight) : query.OrderByDescending(r => r.PricePerNight);
                var rooms = await query.ToListAsync();
                foreach (var room in rooms)
                {
                    _availableRooms.Add(room);
                }
            }
        }

        private async Task LoadBusyRooms(string sort = "CheckoutDate")
        {
            _busyRooms.Clear();
            using (var context = new User001Context())
            {
                var query = context.BusyRoomsTodays.AsQueryable();
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
                        Status = (BookingStatus)Enum.Parse(typeof(BookingStatus), booking.Status), 
                        DisplayCheckoutDate = booking.CheckoutDate.HasValue
                            ? booking.CheckoutDate.Value.ToDateTime(TimeOnly.MinValue)
                            : (DateTime?)null
                    };
                    _busyRooms.Add(viewModel);
                }
            }
            Console.WriteLine($"Loaded {_busyRooms.Count} busy rooms: {string.Join(", ", _busyRooms.Select(b => $"Room {b.RoomNumber} (Client: {b.ClientName}, Checkout: {b.CheckoutDate})"))}");
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
                var fullNameTextBox = new TextBox
                {
                    Text = client.FullName,
                    Name = "FullName",
                    Watermark = "ФИО",
                    Margin = new Thickness(8),
                    FontSize = 14
                };
                var phoneTextBox = new TextBox
                {
                    Text = client.Phone,
                    Name = "Phone",
                    Watermark = "Телефон",
                    Margin = new Thickness(8),
                    FontSize = 14
                };
                var emailTextBox = new TextBox
                {
                    Text = client.Email,
                    Name = "Email",
                    Watermark = "Email",
                    Margin = new Thickness(8),
                    FontSize = 14
                };
                var passportTextBox = new TextBox
                {
                    Text = client.Passport,
                    Name = "Passport",
                    Watermark = "Паспорт", 
                    Margin = new Thickness(8),
                    FontSize = 14
                };
                var saveButton = new Button
                {
                    Content = "Save",
                    Name = "SaveButton",
                    Classes = { "success" }
                };

                var dialog = new Window
                {
                    Title = "Редактирование клиента",
                    Width = 400,
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
                                    Text = "Редактирование клиента",
                                    Classes = { "header" },
                                    TextAlignment = TextAlignment
                                    .Center
                                },
                                fullNameTextBox,
                                phoneTextBox,
                                emailTextBox,
                                passportTextBox,
                                new StackPanel
                                {
                                    Orientation = Orientation
                                    .Horizontal,
                                    HorizontalAlignment = HorizontalAlignment
                                    .Right
                                    ,
                                    Children = { saveButton }
                                }
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
                        await ShowErrorDialog(dialog, "ФИО и телефон обязательны!");
                        return;
                    }
                    client.FullName = fullName;
                    client.Phone = phone;
                    client.Email = emailTextBox.Text;
                    client.Passport = passportTextBox.Text;
                    _context!.Clients.Update(client);
                    await _context.SaveChangesAsync();
                    await LoadClients();
                    await LoadBusyRooms();
                    dialog.Close();
                };

                await dialog.ShowDialog(this);
            }
        }
        private async void ListBusyRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deleteButton = this.FindControl<Button>("DeleteBookingButton");
            deleteButton!.IsEnabled = (sender as ListBox)?.SelectedItem != null;
        }

        private async void ListBusyRooms_DoubleTapped(object sender, TappedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox.SelectedItem is BusyRoomViewModel busyRoom && busyRoom.BookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
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
                await ShowErrorDialog(this, "Выберите клиента!");
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
                                Text = $"История бронирования: {selectedClient.FullName}",
                                Classes = { "header" },
                                TextAlignment = TextAlignment.Center
                            },
                            new ListBox
                            {
                                ItemsSource = bookings,
                                ItemTemplate = new FuncDataTemplate<BookingHistoryViewModel>((item, _) =>
                                {
                                    var grid = new Grid
                                    {
                                        ColumnDefinitions = new ColumnDefinitions("100,120,120,100"),
                                        Background = Brushes.Transparent,
                                        Margin = new Thickness(0, 0, 0, 4)
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
                                        Margin = new Thickness(2),
                                        Foreground = item.Status == "Booked" ?
                                            new SolidColorBrush(Color.Parse("#00a651")) :
                                            new SolidColorBrush(Color.Parse("#f66b60"))
                                    });
                                    return grid;
                                }, true)
                            }
                        }
                    }
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
                await ShowErrorDialog(this, "Выберите бронь!");
                return;
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == selectedBooking.BookingId.Value);
            if (booking != null)
            {
                booking.Status = BookingStatus.Canceled.ToString(); // Исправлено: Преобразование BookingStatus в строку
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
                await LoadAvailableRooms();
                await LoadBusyRooms();
                Console.WriteLine($"Booking {booking.Id} canceled successfully");
            }
        }

        private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GenerateReportDialog(_context);
            await dialog.ShowDialog(this);
        }

        private async void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            var fullNameTextBox = new TextBox
            {
                Name = "FullName",
                Watermark = "ФИО",
                Margin = new Thickness(8),
                FontSize = 14
            };
            var phoneTextBox = new TextBox
            {
                Name = "Phone",
                Watermark = "Телефон",
                Margin = new Thickness(8),
                FontSize = 14
            };
            var emailTextBox = new TextBox
            {
                Name = "Email",
                Watermark = "Email",
                Margin = new Thickness(8),
                FontSize = 14
            };
            var passportTextBox = new TextBox
            {
                Name = "Passport",
                Watermark = "Паспорт",
                Margin = new Thickness(8),
                FontSize = 14
            };
            var saveButton = new Button
            {
                Content = "Сохранить",
                Name = "SaveClientButton",
                Classes = { "success" }
            };

            var dialog = new Window
            {
                Title = "Добавить клиента",
                Width = 400,
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
                                Text = "Добавить клиента",
                                Classes = { "header" },
                                TextAlignment = TextAlignment.Center
                            },
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
                }
            };

            saveButton.Click += async (_, __) =>
            {
                var fullName = fullNameTextBox.Text;
                var phone = phoneTextBox.Text;
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
                {
                    await ShowErrorDialog(dialog, "ФИО и телефон обязательны!");
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
                await LoadBusyRooms();
                dialog.Close();
            };

            await dialog.ShowDialog(this);
        }

        private async void DeleteClientButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = this.FindControl<ListBox>("ListClients").SelectedItem as Client;
            if (selectedClient == null)
            {
                await ShowErrorDialog(this, "Выберите клиента!");
                return;
            }

            var activeBookings = await _context.Bookings
                .CountAsync(b => b.ClientId == selectedClient.Id && b.Status == BookingStatus.Booked.ToString());
            if (activeBookings > 0)
            {
                await ShowErrorDialog(this, "У клиента есть активные брони!");
                return;
            }

            var clientToDelete = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == selectedClient.Id);
            if (clientToDelete == null)
            {
                await ShowErrorDialog(this, "Клиент не найден в базе данных!");
                return;
            }

            _context.Clients.Remove(clientToDelete);
            await _context.SaveChangesAsync();
            await LoadClients();
            await LoadBusyRooms();
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

        private async Task ShowErrorDialog(Window parent, string message)
        {
            var dialog = new Window
            {
                Title = "Ошибка",
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
                                Text = "Ошибка",
                                Classes = { "header" },
                                TextAlignment = TextAlignment.Center,
                                Foreground = new SolidColorBrush(Color.Parse("#f66b60"))
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
            await dialog.ShowDialog(parent);
        }
    }

    public class BusyRoomViewModel
    {
        public int? BookingId { get; set; }
        public string? RoomNumber { get; set; }
        public DateOnly? CheckoutDate { get; set; }
        public string? ClientName { get; set; }
        public BookingStatus Status { get; set; } // Теперь перечисление
        public DateTime? DisplayCheckoutDate { get; set; }
    }
}
