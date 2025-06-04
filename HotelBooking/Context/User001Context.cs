using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelBooking.Models;
using System;

namespace HotelBooking;

public partial class User001Context : DbContext
{
    public User001Context() { }

    public User001Context(DbContextOptions<User001Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AvailableRoomsToday> AvailableRoomsTodays { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<BusyRoomsToday> BusyRoomsTodays { get; set; }
    public virtual DbSet<Client> Clients { get; set; }
    public virtual DbSet<Room> Rooms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=89.110.53.87:5522;Database=user001;Username=user001;Password=78199")
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("boking", "booking_status", new[] { "Booked", "Canceled", "Completed" })
            .HasPostgresExtension("btree_gist");

        modelBuilder.Entity<AvailableRoomsToday>(entity =>
        {
            entity.HasNoKey().ToView("available_rooms_today", "boking");

            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Number).HasMaxLength(50).HasColumnName("number");
            entity.Property(e => e.PricePerNight).HasColumnName("price_per_night");
            entity.Property(e => e.Style).HasMaxLength(50).HasColumnName("style");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookings_pkey");
            entity.ToTable("bookings", "boking");

            entity.HasIndex(e => e.ClientId, "bookings_client_id_idx");
            entity.HasIndex(e => e.RoomId, "bookings_room_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('boking.bookings_id_seq1'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("VARCHAR(50)");
            }); ; // Из строки в перечисление

            entity.HasOne(d => d.Client).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("fk_bookings_client");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_bookings_room");
        });

        modelBuilder.Entity<BusyRoomsToday>(entity =>
        {
            entity.HasNoKey().ToView("busy_rooms_today", "boking");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CheckoutDate).HasColumnName("checkout_date");
            entity.Property(e => e.ClientName).HasMaxLength(255).HasColumnName("client_name");
            entity.Property(e => e.RoomNumber).HasMaxLength(50).HasColumnName("room_number");
            modelBuilder.Entity<BusyRoomsToday>(entity =>
            {
                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("VARCHAR(50)");
            });
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("clients_pkey");
            entity.ToTable("clients", "boking");

            entity.HasIndex(e => e.Phone, "clients_phone_unique").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('boking.clients_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.FullName).HasMaxLength(255).HasColumnName("full_name");
            entity.Property(e => e.Passport).HasMaxLength(50).HasColumnName("passport");
            entity.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rooms_pkey");
            entity.ToTable("rooms", "boking");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('boking.rooms_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Number).HasMaxLength(50).HasColumnName("number");
            entity.Property(e => e.PricePerNight).HasColumnName("price_per_night");
            entity.Property(e => e.Style).HasMaxLength(50).HasColumnName("style");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
