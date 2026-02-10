using FlightTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Data;

/// <summary>
/// Database context for FlightTracker application.
/// </summary>
public class FlightTrackerDbContext : DbContext
{
    public FlightTrackerDbContext(DbContextOptions<FlightTrackerDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Destinations (airports).
    /// </summary>
    public DbSet<Destination> Destinations => Set<Destination>();

    /// <summary>
    /// Target date ranges to track.
    /// </summary>
    public DbSet<TargetDate> TargetDates => Set<TargetDate>();

    /// <summary>
    /// Price check history.
    /// </summary>
    public DbSet<PriceCheck> PriceChecks => Set<PriceCheck>();

    /// <summary>
    /// Junction table for many-to-many relationship between TargetDates and Destinations.
    /// </summary>
    public DbSet<TargetDateDestination> TargetDateDestinations => Set<TargetDateDestination>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Destination
        modelBuilder.Entity<Destination>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AirportCode)
                .IsRequired()
                .HasMaxLength(3);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(e => e.AirportCode)
                .IsUnique();
        });

        // Configure TargetDate
        modelBuilder.Entity<TargetDate>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.OutboundDate)
                .IsRequired();
            
            entity.Property(e => e.ReturnDate)
                .IsRequired();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.HasIndex(e => new { e.OutboundDate, e.ReturnDate })
                .IsUnique();
        });

        // Configure PriceCheck
        modelBuilder.Entity<PriceCheck>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CheckTimestamp)
                .IsRequired();
            
            entity.Property(e => e.Price)
                .IsRequired()
                .HasPrecision(10, 2);
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("EUR");
            
            entity.Property(e => e.DepartureTime)
                .IsRequired();
            
            entity.Property(e => e.ArrivalTime)
                .IsRequired();
            
            entity.Property(e => e.Airline)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Stops)
                .IsRequired();
            
            entity.Property(e => e.BookingUrl)
                .HasMaxLength(500);
            
            // Relationships
            entity.HasOne(e => e.TargetDate)
                .WithMany(t => t.PriceChecks)
                .HasForeignKey(e => e.TargetDateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Destination)
                .WithMany(d => d.PriceChecks)
                .HasForeignKey(e => e.DestinationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for performance
            entity.HasIndex(e => new { e.TargetDateId, e.DestinationId });
            entity.HasIndex(e => e.CheckTimestamp);
        });

        // Configure TargetDateDestination
        modelBuilder.Entity<TargetDateDestination>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            // Relationships
            entity.HasOne(e => e.TargetDate)
                .WithMany(t => t.TargetDateDestinations)
                .HasForeignKey(e => e.TargetDateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Destination)
                .WithMany(d => d.TargetDateDestinations)
                .HasForeignKey(e => e.DestinationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Unique constraint: each destination can only be associated once per target date
            entity.HasIndex(e => new { e.TargetDateId, e.DestinationId })
                .IsUnique();
        });
    }
}
