using EventRegistration.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EventRegistration.Infrastructure;
public sealed class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEventEntity(modelBuilder);
        ConfigureRestrationEntity(modelBuilder);
        ApplyGlobalQueryFilters(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        //Query performance optimization
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableSensitiveDataLogging(false)
            .EnableDetailedErrors(false);
    }

    private static void ConfigureEventEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            //Primary Key
            entity.HasKey(e => e.Id);

            // Relationships
            entity.HasMany(e => e.Registrations)
                .WithOne(r => r.Event)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Value conversions for UTC consistency
            entity.Property(e => e.StartTime)
                .HasConversion(new ValueConverter<DateTime, DateTime>(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));

            entity.Property(e => e.EndTime)
                .HasConversion(new ValueConverter<DateTime, DateTime>(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));

            entity.Property(e => e.CreatedAt)
                .HasConversion(new ValueConverter<DateTime, DateTime>(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));

            // Indexes (defined in entity attributes, but can be configured here too)
            entity.HasIndex(e => e.StartTime).HasDatabaseName("IX_Events_StartTime");
            entity.HasIndex(e => e.EndTime).HasDatabaseName("IX_Events_EndTime");
            entity.HasIndex(e => e.CreatedBy).HasDatabaseName("IX_Events_CreatedBy");
            entity.HasIndex(e => new { e.StartTime, e.EndTime }).HasDatabaseName("IX_Events_DateRange");
            // Composite index for keyset pagination
            entity.HasIndex(e => new { e.StartTime, e.Id }).HasDatabaseName("IX_Events_StartTime_Id");

            // Ignored properties (computed)
            entity.Ignore(e => e.RegistrationCount);
        });
    }

    private static void ConfigureRestrationEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Registration>(entity =>
        {
            // Primary Key
            entity.HasKey(r => r.Id);
            // Indexes
            entity.HasIndex(r => r.EventId).HasDatabaseName("IX_Registrations_EventId");
            entity.HasIndex(r => r.EmailAddress).HasDatabaseName("IX_Registrations_EmailAddress");
            entity.HasIndex(r => new { r.EventId, r.EmailAddress })
                .IsUnique()
                .HasDatabaseName("IX_Registrations_EventId_Email");
            entity.HasIndex(r => r.RegisteredAt).HasDatabaseName("IX_Registrations_RegisteredAt");
            // Keyset pagination index
            entity.HasIndex(r => new { r.EventId, r.RegisteredAt, r.Id })
                .HasDatabaseName("IX_Registrations_EventId_RegisteredAt_Id");

            // DateTime conversions to UTC
            entity.Property(r => r.RegisteredAt)
                .HasConversion(new ValueConverter<DateTime, DateTime>(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
        });
    }

    private void ConvertDatesToUtc()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime && dateTime.Kind != DateTimeKind.Utc)
                {
                    property.CurrentValue = dateTime.ToUniversalTime();
                }
            }
        }
    }

    private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Registration>().HasQueryFilter(r => !r.IsDeleted);
    }

    public override int SaveChanges()
    {
        // Override DateTime to UTC
        ConvertDatesToUtc();

        return base.SaveChanges();
    }

    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Override DateTime to UTC
        ConvertDatesToUtc();

        return base.SaveChangesAsync(cancellationToken);
    }
}
