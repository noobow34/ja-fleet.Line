using System;
using JafleetLine.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace jafleetline.EF
{
    public partial class jafleetContext : DbContext
    {
        public jafleetContext()
        {
        }

        public jafleetContext(DbContextOptions<jafleetContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AircraftView> AircraftView { get; set; }
        public virtual DbSet<Log> Log { get; set; }

        public static readonly LoggerFactory MyLoggerFactory
            = new LoggerFactory(new[]
            {
            new ConsoleLoggerProvider((category, level)
                => category == DbLoggerCategory.Database.Command.Name
                && level == LogLevel.Information, true)
            });


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseLoggerFactory(MyLoggerFactory).UseSqlite("Data Source='../ja-fleet_db/ja-fleet.sqlite3'");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(e => e.LogId);

                entity.ToTable("log");

                entity.Property(e => e.LogId)
                .HasColumnName("LOG_ID")
                .ValueGeneratedOnAdd();

                entity.Property(e => e.LogDate).HasColumnName("LOG_DATE");

                entity.Property(e => e.LogType).HasColumnName("LOG_TYPE");

                entity.Property(e => e.LogDetail).HasColumnName("LOG_DETAIL");

                entity.Property(e => e.UserId).HasColumnName("USER_ID");
            });

            modelBuilder.Entity<AircraftView>(entity =>
            {
                entity.HasKey(e => e.RegistrationNumber);

                entity.ToTable("aircraft_view");

                entity.Property(e => e.RegistrationNumber)
                    .HasColumnName("REGISTRATION_NUMBER")
                    .ValueGeneratedNever();

                entity.Property(e => e.Airline).HasColumnName("AIRLINE");

                entity.Property(e => e.AirlineGroupCode).HasColumnName("AIRLINE_GROUP_CODE");

                entity.Property(e => e.AirlineNameJpShort).HasColumnName("AIRLINE_NAME_JP_SHORT");

                entity.Property(e => e.CreationTime).HasColumnName("CREATION_TIME");

                entity.Property(e => e.DisplayOrder).HasColumnName("DISPLAY_ORDER");

                entity.Property(e => e.Operation).HasColumnName("OPERATION");

                entity.Property(e => e.OperationCode).HasColumnName("OPERATION_CODE");

                entity.Property(e => e.RegisterDate).HasColumnName("REGISTER_DATE");

                entity.Property(e => e.Remarks).HasColumnName("REMARKS");

                entity.Property(e => e.SerialNumber).HasColumnName("SERIAL_NUMBER");

                entity.Property(e => e.TypeCode).HasColumnName("TYPE_CODE");

                entity.Property(e => e.TypeName).HasColumnName("TYPE_NAME");

                entity.Property(e => e.TypeDetailCode).HasColumnName("TYPE_DETAIL_CODE");

                entity.Property(e => e.TypeDetailName).HasColumnName("TYPE_DETAIL_NAME");

                entity.Property(e => e.UpdateTime).HasColumnName("UPDATE_TIME");

                entity.Property(e => e.Wifi).HasColumnName("WIFI");

                entity.Property(e => e.WifiCode).HasColumnName("WIFI_CODE");

                entity.Property(e => e.LinkUrl).HasColumnName("LINK_URL");

                entity.Property(e => e.ActualUpdateTime).HasColumnName("ACTUAL_UPDATE_TIME");
            });
        }
    }
}
