using System;

using System.Collections.Generic;

using HotelBooking.Models;

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;



namespace HotelBooking.Context;



public partial class User001Context : DbContext

{

    public User001Context()

    {

    }



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

#pragma warning disable CS0162 // Unreachable code detected

#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.

        => optionsBuilder.UseNpgsql("Host=89.110.53.87:5522;Database=user001;Username=user001;password=78199");

#pragma warning restore CS0162 // Unreachable code detected



    protected override void OnModelCreating(ModelBuilder modelBuilder)

    {

        modelBuilder

            .HasPostgresEnum("boking", "booking_status", new[] { "Booked", "Canceled", "Completed" })

            .HasPostgresExtension("btree_gist");



        modelBuilder.Entity<AvailableRoomsToday>(entity =>

        {

            entity

                .HasNoKey()

                .ToView("available_rooms_today", "boking");



            entity.Property(e => e.Capacity).HasColumnName("capacity");

            entity.Property(e => e.Description).HasColumnName("description");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.Property(e => e.Number)

                .HasMaxLength(50)

                .HasColumnName("number");

            entity.Property(e => e.PricePerNight)

                .HasPrecision(10, 2)

                .HasColumnName("price_per_night");

            entity.Property(e => e.Style)

                .HasMaxLength(100)

                .HasColumnName("style");

        });



        modelBuilder.Entity<Booking>(entity =>

        {

            entity.HasKey(e => e.Id).HasName("bookings_pkey");



            entity.ToTable("bookings", "boking");



            entity.HasIndex(e => e.ClientId, "bookings_client_id_idx");

            entity.HasIndex(e => e.RoomId, "bookings_room_id_idx");



            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.ClientId).HasColumnName("client_id");

            entity.Property(e => e.CreatedAt)

                .HasDefaultValueSql("now()")

                .HasColumnName("created_at");

            entity.Property(e => e.EndDate).HasColumnName("end_date");

            entity.Property(e => e.RoomId).HasColumnName("room_id");

            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.Property(e => e.Status)

                .HasDefaultValueSql("'Booked'::boking.booking_status")

                .HasColumnName("status")

                .HasColumnType("boking.booking_status")

                .HasConversion(

                    v => v.ToString(),

                    v => Enum.Parse<BookingStatus>(v));



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

            entity

                .HasNoKey()

                .ToView("busy_rooms_today", "boking");



            entity.Property(e => e.BookingId).HasColumnName("booking_id");

            entity.Property(e => e.CheckoutDate).HasColumnName("checkout_date");

            entity.Property(e => e.ClientName)

                .HasMaxLength(255)

                .HasColumnName("client_name");

            entity.Property(e => e.RoomNumber)

                .HasMaxLength(50)

                .HasColumnName("room_number");

            entity.Property(e => e.Status)

                .HasColumnName("status")

                .HasColumnType("boking.booking_status")

                .HasConversion(

                    v => v.ToString(),

                    v => Enum.Parse<BookingStatus>(v));

        });



        modelBuilder.Entity<Client>(entity =>

        {

            entity.HasKey(e => e.Id).HasName("clients_pkey");



            entity.ToTable("clients", "boking");



            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.CreatedAt)

                .HasDefaultValueSql("now()")

                .HasColumnName("created_at");

            entity.Property(e => e.Email)

                .HasMaxLength(255)

                .HasColumnName("email");

            entity.Property(e => e.FullName)

                .HasMaxLength(255)

                .HasColumnName("full_name");

            entity.Property(e => e.Passport)

                .HasMaxLength(100)

                .HasColumnName("passport");

            entity.Property(e => e.Phone)

                .HasMaxLength(50)

                .HasColumnName("phone");

        });



        modelBuilder.Entity<Room>(entity =>

        {

            entity.HasKey(e => e.Id).HasName("rooms_pkey");



            entity.ToTable("rooms", "boking");



            entity.HasIndex(e => e.Number, "rooms_number_key").IsUnique();



            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Capacity).HasColumnName("capacity");

            entity.Property(e => e.Description).HasColumnName("description");

            entity.Property(e => e.IsActive)

                .HasDefaultValue(true)

                .HasColumnName("is_active");

            entity.Property(e => e.Number)

                .HasMaxLength(50)

                .HasColumnName("number");

            entity.Property(e => e.PricePerNight)

                .HasPrecision(10, 2)

                .HasColumnName("price_per_night");

            entity.Property(e => e.Style)

                .HasMaxLength(100)

                .HasColumnName("style");

        });



        OnModelCreatingPartial(modelBuilder);

    }



    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

}