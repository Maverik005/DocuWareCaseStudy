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
using Xunit;
using Xunit.Abstractions;

namespace EventRegistrationTests.Tests;

public class RegistrationKeysetPaginationTests: IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private ApplicationDbContext _context = null!;
    private RegistrationRepository _repository = null!;
    private RegistrationCache _cache = null!;
    private int _testEventId;
    private const int TotalRegistrations = 100000;

    public RegistrationKeysetPaginationTests(ITestOutputHelper output)
    {
        _output = output; 
    }

    public async Task InitializeAsync()
    {
        //Set options
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"RegistrationPaginationTest_{Guid.NewGuid()}")
            .Options;

        // Create EventCache with IMemoryCache
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new RegistrationCache(memoryCache);

        //create context and repository
        _context = new ApplicationDbContext(options);
        _repository = new RegistrationRepository(_context, _cache);

        //create test event for registrations
        var testEvent = new Event
        {
            Name = "Large Test Event",
            Description = "Event for pagination testing with 100K registrations",
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

        //seed 100K registrations
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
    /// Test to demonstrate O(1) consistent performance upto 1000 pages
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public async Task KeysetPagination_ExtremeDepth_MaintainsPerformance(int targetPage)
    {
        // Arrange
        var pageSize = 100;
        int? lastId = 1;
        DateTime? lastRegisteredAt = DateTime.UtcNow.AddDays(-30);

        // Warm up query to cache count - First Registrations query will always be slow
        _output.WriteLine($"Warming up cache for page 1...");

        var warmupParams = new RegistrationSearchParameters
        {
            EventId = _testEventId,
            PageSize = 10
        };
        var warmupResult = await _repository.GetRegistrationsByEventIdAsync(warmupParams); // Once the totalCount is cached subsequent queries will be fast

        // Navigate to target page
        for (int i = 1; i < targetPage; i++)
        {
            var navParams = new RegistrationSearchParameters
            {
                EventId = _testEventId,
                PageSize = pageSize,
                LastId = lastId,
                LastRegisteredAt = lastRegisteredAt
            };

            var navResult = await _repository.GetRegistrationsByEventIdAsync(navParams);

            if (!navResult.HasNextPage)
                break;

            var lastItem = navResult.Items.Last();
            lastId = lastItem.Id;
            lastRegisteredAt = lastItem.RegisteredAt;
        }

        var parameters = new RegistrationSearchParameters
        {
            EventId = _testEventId,
            PageSize = pageSize,
            LastId = lastId,
            LastRegisteredAt = lastRegisteredAt
        };

        // Act
        var sw = Stopwatch.StartNew();
        var result = await _repository.GetRegistrationsByEventIdAsync(parameters);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(200);

        _output.WriteLine($"✓ Page {targetPage,4}: {sw.ElapsedMilliseconds,3}ms ({result.Items.Count} items)");

        // KEY INSIGHT: Page 1000 (skipping 99,900 records) is just as fast as Page 1
    }
}
