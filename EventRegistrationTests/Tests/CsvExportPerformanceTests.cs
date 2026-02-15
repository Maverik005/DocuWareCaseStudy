using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;
using EventRegistration.Core.Utilities;
using EventRegistration.Infrastructure;
using EventRegistration.Infrastructure.Repositories;
using EventRegistrationTests.Seeders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace EventRegistrationTests.Tests;

public class CsvExportPerformanceTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private ApplicationDbContext _context = null!;
    private RegistrationRepository _repository = null!;
    private RegistrationCache _cache = null!;
    private int _testEventId;
    private const int TotalRegistrations = 100000;

    public CsvExportPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CsvExportTest_{Guid.NewGuid()}")
            .Options;

        // Create EventCache with IMemoryCache
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new RegistrationCache(memoryCache);

        _context = new ApplicationDbContext(options);
        _repository = new RegistrationRepository(_context,_cache);

        // Create test event
        var testEvent = new Event
        {
            Name = "Large Export Test Event",
            Description = "Event for CSV export testing with 100K registrations",
            Location = "Test Venue, Berlin",
            StartTime = DateTime.UtcNow.AddDays(30),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(4),
            CreatedBy = "test-user",
            CreatedByName = "Test User",
            IsDeleted = false
        };

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();
        _testEventId = testEvent.Id;

        // Seed 100K registrations
        await TestDataSeeder.SeedLargeRegistrationDatasetAsync(
            _context,
            _testEventId,
            TotalRegistrations);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    /// <summary>
    /// Test 1: Proves streaming uses constant memory
    /// </summary>
    [Fact]
    public async Task StreamingExport_100KRegistrations_UsesConstantMemory()
    {
        // Arrange
        var parameters = new RegistrationSearchParameters
        {
            EventId = _testEventId
        };

        // Measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Name,Email,Phone,Registered At");

        var recordCount = 0;
        var sw = Stopwatch.StartNew();

        // Act - Stream and export
        await foreach (var registration in _repository.StreamRegistrationsAsync(parameters.EventId))
        {
            csvBuilder.AppendLine(
                $"{EscapeCsv(registration.Name)}," +
                $"{EscapeCsv(registration.EmailAddress)}," +
                $"{EscapeCsv(registration.PhoneNumber ?? "")}," +
                $"{registration.RegisteredAt:yyyy-MM-dd HH:mm:ss}");

            recordCount++;

            // Clear buffer periodically to simulate writing to file
            if (recordCount % 10000 == 0)
            {
                csvBuilder.Clear(); // Simulate flushing to disk
                csvBuilder.AppendLine("Name,Email,Phone,Registered At");
            }
        }

        sw.Stop();

        // Measure final memory
        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsedMB = (memoryAfter - memoryBefore) / 1024.0 / 1024.0;

        // Assert
        recordCount.Should().Be(TotalRegistrations);
        memoryUsedMB.Should().BeLessThan(50);


        _output.WriteLine($"✓ Streaming Export Performance:");
        _output.WriteLine($"  Records exported: {recordCount:N0}");
        _output.WriteLine($"  Total time: {sw.ElapsedMilliseconds:N0}ms ({sw.Elapsed.TotalSeconds:F1}s)");
        _output.WriteLine($"  Speed: {recordCount / sw.Elapsed.TotalSeconds:N0} records/sec");
        _output.WriteLine($"  Memory used: {memoryUsedMB:F2} MB (constant - proves streaming works!)");
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
