using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Data;
using FlightTracker.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlightTracker.IntegrationTests;

/// <summary>
/// Integration tests for travel date management with soft delete.
/// </summary>
public class TargetDateManagementTests : IDisposable
{
    private readonly FlightTrackerDbContext _context;
    private readonly ITargetDateRepository _repository;

    public TargetDateManagementTests()
    {
        var options = new DbContextOptionsBuilder<FlightTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FlightTrackerDbContext(options);
        _repository = new TargetDateRepository(_context);
    }

    [Fact]
    public async Task CompleteWorkflow_AddDeleteReactivate_ShouldWorkCorrectly()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Summer Vacation",
            OutboundDate = new DateTime(2026, 7, 1),
            ReturnDate = new DateTime(2026, 7, 10)
        };

        // Act 1: Create a new travel date
        var created = await _repository.CreateTargetDateAsync(targetDate);

        // Assert 1: Verify it was created
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("Summer Vacation", created.Name);
        Assert.False(created.IsDeleted);
        Assert.Null(created.DeletedAt);

        // Act 2: Get all active dates (should include our new date)
        var activeDates = await _repository.GetAllAsync();
        
        // Assert 2: Verify it appears in active dates
        Assert.Single(activeDates);
        Assert.Contains(activeDates, d => d.Id == created.Id);

        // Act 3: Soft delete the date
        var deleteResult = await _repository.SoftDeleteAsync(created.Id);

        // Assert 3: Verify soft delete succeeded
        Assert.True(deleteResult);

        // Act 4: Get all active dates (should be empty now)
        var activeDatesAfterDelete = await _repository.GetAllAsync();
        
        // Assert 4: Verify it's no longer in active dates
        Assert.Empty(activeDatesAfterDelete);

        // Act 5: Get deleted dates
        var deletedDates = await _repository.GetDeletedAsync();

        // Assert 5: Verify it appears in deleted dates
        Assert.Single(deletedDates);
        var deletedDate = deletedDates.First();
        Assert.Equal(created.Id, deletedDate.Id);
        Assert.True(deletedDate.IsDeleted);
        Assert.NotNull(deletedDate.DeletedAt);

        // Act 6: Restore the deleted date
        var restoreResult = await _repository.RestoreAsync(created.Id);

        // Assert 6: Verify restore succeeded
        Assert.True(restoreResult);

        // Act 7: Get all active dates (should include our restored date)
        var activeDatesAfterRestore = await _repository.GetAllAsync();

        // Assert 7: Verify it's back in active dates
        Assert.Single(activeDatesAfterRestore);
        var restoredDate = activeDatesAfterRestore.First();
        Assert.Equal(created.Id, restoredDate.Id);
        Assert.False(restoredDate.IsDeleted);
        Assert.Null(restoredDate.DeletedAt);

        // Act 8: Get deleted dates (should be empty now)
        var deletedDatesAfterRestore = await _repository.GetDeletedAsync();

        // Assert 8: Verify it's no longer in deleted dates
        Assert.Empty(deletedDatesAfterRestore);
    }

    [Fact]
    public async Task SoftDelete_AlreadyDeleted_ShouldReturnFalse()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Weekend Trip",
            OutboundDate = DateTime.Today.AddDays(30),
            ReturnDate = DateTime.Today.AddDays(33)
        };

        var created = await _repository.CreateTargetDateAsync(targetDate);
        await _repository.SoftDeleteAsync(created.Id);

        // Act: Try to soft delete again
        var secondDeleteResult = await _repository.SoftDeleteAsync(created.Id);

        // Assert: Should return false (already deleted)
        Assert.False(secondDeleteResult);
    }

    [Fact]
    public async Task Restore_NotDeleted_ShouldReturnFalse()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Business Trip",
            OutboundDate = DateTime.Today.AddDays(14),
            ReturnDate = DateTime.Today.AddDays(17)
        };

        var created = await _repository.CreateTargetDateAsync(targetDate);

        // Act: Try to restore a non-deleted date
        var restoreResult = await _repository.RestoreAsync(created.Id);

        // Assert: Should return false (not deleted)
        Assert.False(restoreResult);
    }

    [Fact]
    public async Task UpdateTargetDate_DeletedDate_ShouldReturnFalse()
    {
        // Arrange
        var targetDate = new TargetDate
        {
            Name = "Conference",
            OutboundDate = DateTime.Today.AddDays(60),
            ReturnDate = DateTime.Today.AddDays(63)
        };

        var created = await _repository.CreateTargetDateAsync(targetDate);
        await _repository.SoftDeleteAsync(created.Id);

        // Act: Try to update a deleted date
        created.Name = "Updated Conference";
        var updateResult = await _repository.UpdateTargetDateAsync(created);

        // Assert: Should return false (date is deleted)
        Assert.False(updateResult);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedByDefault()
    {
        // Arrange
        var activeDate1 = new TargetDate
        {
            Name = "Active Date 1",
            OutboundDate = DateTime.Today.AddDays(10),
            ReturnDate = DateTime.Today.AddDays(13)
        };

        var activeDate2 = new TargetDate
        {
            Name = "Active Date 2",
            OutboundDate = DateTime.Today.AddDays(20),
            ReturnDate = DateTime.Today.AddDays(23)
        };

        var deletedDate = new TargetDate
        {
            Name = "Deleted Date",
            OutboundDate = DateTime.Today.AddDays(30),
            ReturnDate = DateTime.Today.AddDays(33)
        };

        await _repository.CreateTargetDateAsync(activeDate1);
        await _repository.CreateTargetDateAsync(activeDate2);
        var deleted = await _repository.CreateTargetDateAsync(deletedDate);
        await _repository.SoftDeleteAsync(deleted.Id);

        // Act
        var activeDates = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, activeDates.Count());
        Assert.All(activeDates, d => Assert.False(d.IsDeleted));
    }

    [Fact]
    public async Task GetAllIncludingDeletedAsync_ReturnsAllDates()
    {
        // Arrange
        var activeDate = new TargetDate
        {
            Name = "Active",
            OutboundDate = DateTime.Today.AddDays(10),
            ReturnDate = DateTime.Today.AddDays(13)
        };

        var deletedDate = new TargetDate
        {
            Name = "Deleted",
            OutboundDate = DateTime.Today.AddDays(20),
            ReturnDate = DateTime.Today.AddDays(23)
        };

        await _repository.CreateTargetDateAsync(activeDate);
        var deleted = await _repository.CreateTargetDateAsync(deletedDate);
        await _repository.SoftDeleteAsync(deleted.Id);

        // Act
        var allDates = await _repository.GetAllIncludingDeletedAsync();

        // Assert
        Assert.Equal(2, allDates.Count());
        Assert.Contains(allDates, d => !d.IsDeleted);
        Assert.Contains(allDates, d => d.IsDeleted);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
